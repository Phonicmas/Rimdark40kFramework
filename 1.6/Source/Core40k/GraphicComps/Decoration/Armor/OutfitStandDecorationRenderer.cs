using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using ApparelLayerDefOf = RimWorld.ApparelLayerDefOf;

namespace Core40k;

/// <summary>
/// Draws whatever the registered OutfitStandDrawProviders contribute for the apparel an outfit
/// stand is displaying. This owns the per stand cache, the layer bookkeeping and the draw call; the
/// providers own what actually gets drawn.
/// </summary>
public static class OutfitStandDecorationRenderer
{
    //The stand draws all apparel on a 1.5 mesh, the same size a humanlike body node uses, so
    //draw sizes carry over from the pawn unchanged.
    private const float StandMeshSize = 1.5f;

    private class StandCache
    {
        public readonly List<OutfitStandDraw>[] body = new List<OutfitStandDraw>[4];
        public readonly List<OutfitStandDraw>[] head = new List<OutfitStandDraw>[4];
        public readonly List<OutfitStandDraw>[] overHead = new List<OutfitStandDraw>[4];

        public BodyTypeDef bodyType = BodyTypeDefOf.Male;
        public bool skipHead;

        public StandCache()
        {
            for (var i = 0; i < 4; i++)
            {
                body[i] = [];
                head[i] = [];
                overHead[i] = [];
            }
        }

        public bool Any
        {
            get
            {
                for (var i = 0; i < 4; i++)
                {
                    if (body[i].Count > 0 || head[i].Count > 0 || overHead[i].Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void SortByLayer()
        {
            for (var i = 0; i < 4; i++)
            {
                body[i].SortBy(draw => draw.layer);
                head[i].SortBy(draw => draw.layer);
                overHead[i].SortBy(draw => draw.layer);
            }
        }
    }

    private static readonly ConditionalWeakTable<Building_OutfitStand, StandCache> caches = new();

    private static bool loggedFailure;

    private static bool loggedProviderFailure;

    public static void Invalidate(Building_OutfitStand stand)
    {
        if (stand != null)
        {
            caches.Remove(stand);
        }
    }

    /// <summary>
    /// Called when a decorated item changes appearance, in case it is sitting in a stand rather than
    /// being worn. The stand's own recache already covers items being put in and taken out.
    /// </summary>
    public static void Notify_ItemGraphicChanged(Thing thing)
    {
        if (thing?.ParentHolder is Building_OutfitStand stand)
        {
            Invalidate(stand);
        }
    }

    public static void Draw(Building_OutfitStand stand, Vector3 drawLoc, Rot4 rot)
    {
        try
        {
            DrawInner(stand, drawLoc, rot);
        }
        catch (Exception exception)
        {
            if (loggedFailure)
            {
                return;
            }

            loggedFailure = true;
            Log.Error("[Core40k] Failed to draw outfit stand decorations, further failures will not be logged: " + exception);
        }
    }

    private static void DrawInner(Building_OutfitStand stand, Vector3 drawLoc, Rot4 rot)
    {
        if (stand == null)
        {
            return;
        }

        var cache = caches.GetValue(stand, BuildCache);
        if (!cache.Any)
        {
            return;
        }

        var mesh = MeshPool.GetMeshSetForSize(StandMeshSize, StandMeshSize).MeshAt(rot);

        DrawList(cache.body[rot.AsInt], mesh, rot, drawLoc, AltitudeLayer.Item.AltitudeFor());

        if (!cache.skipHead)
        {
            DrawList(cache.head[rot.AsInt], mesh, rot, drawLoc + HeadOffsetAt(cache.bodyType, rot), AltitudeLayer.ItemImportant.AltitudeFor());
        }

        //Body positioned, but in the headgear band so it can sit over the helmet - and over the
        //stand's own head graphic, which is drawn just under that band.
        DrawList(cache.overHead[rot.AsInt], mesh, rot, drawLoc, AltitudeLayer.ItemImportant.AltitudeFor());
    }

    private static void DrawList(List<OutfitStandDraw> draws, Mesh mesh, Rot4 rot, Vector3 origin, float baseAltitude)
    {
        foreach (var draw in draws)
        {
            var material = draw.graphic.MatAt(draw.flipMaterial && rot.IsHorizontal ? rot.Opposite : rot);
            if (material == null)
            {
                continue;
            }

            var position = origin + draw.offset;
            position.y = baseAltitude + PawnRenderUtility.AltitudeForLayer(draw.layer);

            var matrix = Matrix4x4.TRS(position, Quaternion.AngleAxis(draw.angle, Vector3.up), draw.scale);
            Graphics.DrawMesh(draw.flipMesh ? MeshPool.GridPlaneFlip(mesh) : mesh, matrix, material, 0);
        }
    }

    private static StandCache BuildCache(Building_OutfitStand stand)
    {
        var cache = new StandCache();

        var standBodyType = BodyTypeFor(stand);
        var isChild = standBodyType == BodyTypeDefOf.Child;
        var bodySizeFactor = (standBodyType.bodyGraphicScale.x + standBodyType.bodyGraphicScale.y) / 2f;
        cache.bodyType = standBodyType;

        var apparels = new List<Apparel>();
        foreach (var thing in stand.HeldItems)
        {
            if (thing is Apparel apparel)
            {
                apparels.Add(apparel);
            }
        }

        if (apparels.Count == 0)
        {
            return cache;
        }

        apparels.SortBy(apparel => apparel.def.apparel.LastLayer.drawOrder);

        //Mirrors the layer numbering in Building_OutfitStand.RecacheGraphics so contributed quads
        //land in the same depth space as the apparel they belong to.
        var headApparelTopLayer = HeadApparelTopLayer(apparels);

        var layerCounts = new Dictionary<ApparelLayerDef, int>();
        foreach (var apparel in apparels)
        {
            if (apparel.def.apparel.renderSkipFlags.NotNullAndContains(RenderSkipFlagDefOf.Head))
            {
                cache.skipHead = true;
            }

            if (apparel.WornGraphicPath.NullOrEmpty())
            {
                continue;
            }

            var lastLayer = apparel.def.apparel.LastLayer;
            var headgear = lastLayer == ApparelLayerDefOf.Overhead || lastLayer == ApparelLayerDefOf.EyeCover;

            var seen = layerCounts.TryGetValue(lastLayer, out var count) ? count : 0;
            layerCounts[lastLayer] = seen + 1;

            var apparelBaseLayer = lastLayer.drawOrder / 10 + seen;

            var context = new OutfitStandDrawContext(
                stand,
                apparel,
                standBodyType,
                isChild,
                bodySizeFactor,
                headgear,
                apparelBaseLayer,
                headApparelTopLayer,
                ApparelLayersForRot(apparel, apparelBaseLayer, headgear),
                cache.body,
                cache.head,
                cache.overHead,
                () => cache.skipHead = true);

            foreach (var provider in OutfitStandDrawProvider.Providers)
            {
                try
                {
                    provider.CollectDraws(context);
                }
                catch (Exception exception)
                {
                    if (loggedProviderFailure)
                    {
                        continue;
                    }

                    loggedProviderFailure = true;
                    Log.Error($"[Core40k] Outfit stand draw provider {provider.GetType().FullName} failed on {apparel.def.defName}, further failures will not be logged: {exception}");
                }
            }
        }

        cache.SortByLayer();
        return cache;
    }

    /// <summary>
    /// The highest layer vanilla's own RecacheGraphics gives a headgear quad on this stand, across
    /// all four rotations. Quads that belong over the helmet are stacked on top of this.
    /// </summary>
    private static float HeadApparelTopLayer(List<Apparel> apparels)
    {
        var top = 0f;
        var layerCounts = new Dictionary<ApparelLayerDef, int>();

        foreach (var apparel in apparels)
        {
            var lastLayer = apparel.def.apparel.LastLayer;
            if (lastLayer != ApparelLayerDefOf.Overhead && lastLayer != ApparelLayerDefOf.EyeCover)
            {
                continue;
            }

            if (apparel.WornGraphicPath.NullOrEmpty())
            {
                continue;
            }

            var seen = layerCounts.TryGetValue(lastLayer, out var count) ? count : 0;
            layerCounts[lastLayer] = seen + 1;

            foreach (var layer in ApparelLayersForRot(apparel, lastLayer.drawOrder / 10 + seen, true))
            {
                top = Mathf.Max(top, layer);
            }
        }

        return top;
    }

    private static Func<Building_OutfitStand, BodyTypeDef> bodyTypeGetter;
    private static bool bodyTypeGetterResolved;

    /// <summary>
    /// Building_OutfitStand.BodyTypeDefForRendering is protected, and a modded stand may override it,
    /// so it is read through the virtual getter rather than assumed from the type.
    /// </summary>
    private static BodyTypeDef BodyTypeFor(Building_OutfitStand stand)
    {
        if (!bodyTypeGetterResolved)
        {
            bodyTypeGetterResolved = true;
            try
            {
                var getter = AccessTools.PropertyGetter(typeof(Building_OutfitStand), "BodyTypeDefForRendering");
                if (getter != null)
                {
                    bodyTypeGetter = AccessTools.MethodDelegate<Func<Building_OutfitStand, BodyTypeDef>>(getter);
                }
            }
            catch (Exception exception)
            {
                bodyTypeGetter = null;
                Log.Warning("[Core40k] Could not read Building_OutfitStand.BodyTypeDefForRendering, falling back to the vanilla body types: " + exception);
            }
        }

        BodyTypeDef bodyType = null;
        if (bodyTypeGetter != null)
        {
            bodyType = bodyTypeGetter(stand);
        }

        return bodyType ?? (stand is Building_KidOutfitStand ? BodyTypeDefOf.Child : BodyTypeDefOf.Male);
    }

    /// <summary>Building_OutfitStand.HeadOffsetAt is private; this is the same offset.</summary>
    private static Vector3 HeadOffsetAt(BodyTypeDef bodyType, Rot4 rotation)
    {
        var headOffset = (bodyType ?? BodyTypeDefOf.Male).headOffset;
        return rotation.AsInt switch
        {
            0 => new Vector3(0f, 0f, headOffset.y),
            1 => new Vector3(headOffset.x, 0f, headOffset.y),
            2 => new Vector3(0f, 0f, headOffset.y),
            3 => new Vector3(0f - headOffset.x, 0f, headOffset.y),
            _ => Vector3.zero,
        };
    }

    private static float[] ApparelLayersForRot(Apparel apparel, int apparelBaseLayer, bool headgear)
    {
        var renderAsPack = apparel.RenderAsPack();
        var apparelDrawData = apparel.def.apparel.drawData;
        var layers = new float[4];

        for (var i = 0; i < 4; i++)
        {
            var rot = new Rot4(i);
            var layer = (float)apparelBaseLayer;

            var fromDrawData = apparelDrawData?.LayerForRot(rot, -1f);
            if (fromDrawData is > 0f)
            {
                layer += ((int)fromDrawData.Value - (headgear ? 70 : 20)) * 2;
            }

            if (renderAsPack)
            {
                if (rot == Rot4.North)
                {
                    layer = 93f;
                }
                else if (rot == Rot4.South)
                {
                    layer = -3f;
                }
            }

            layers[i] = layer;
        }

        return layers;
    }
}
