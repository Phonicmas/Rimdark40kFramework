using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Core40k;

/// <summary>
/// Contributes quads to an outfit stand for one piece of held apparel. Subclasses are found and
/// instantiated automatically, so a module only has to declare one - no def to write, and no
/// reference back from the framework to the module.
/// </summary>
public abstract class OutfitStandDrawProvider
{
    private static List<OutfitStandDrawProvider> providers;

    public static List<OutfitStandDrawProvider> Providers
    {
        get
        {
            if (providers != null)
            {
                return providers;
            }

            providers = [];
            foreach (var type in typeof(OutfitStandDrawProvider).AllSubclassesNonAbstract())
            {
                try
                {
                    providers.Add((OutfitStandDrawProvider)Activator.CreateInstance(type));
                }
                catch (Exception exception)
                {
                    Log.Error($"[Core40k] Could not create outfit stand draw provider {type.FullName}: {exception}");
                }
            }

            providers.SortBy(provider => provider.Order);
            return providers;
        }
    }

    public virtual int Order => 0;

    public abstract void CollectDraws(OutfitStandDrawContext context);

    protected static float PawnLayerFor(PawnRenderNodeProperties props, Rot4 rot)
    {
        return props.drawData?.LayerForRot(rot, props.baseLayer) ?? props.baseLayer;
    }

    /// <summary>
    /// Builds the quad for a body space render node and puts it in the band its layer belongs to.
    /// A node authored above the head apparel root draws over the helmet on a pawn, and the stand
    /// keeps headgear a whole altitude layer above the body, so it has to leave the body band or
    /// vanilla's helmet covers it no matter what layer it is given.
    /// </summary>
    protected static void AddNodeDraw(OutfitStandDrawContext context, PawnRenderNodeProperties props, Graphic graphic, Rot4 rot, Vector3? offsetOverride = null, bool? flipOverride = null)
    {
        var pawnLayer = PawnLayerFor(props, rot);
        var overHead = context.DrawsOverHeadApparel(pawnLayer);
        var layer = overHead ? context.OverHeadLayerFor(pawnLayer) : context.StandLayerFor(pawnLayer, false);

        var draw = MakeDraw(context, props, graphic, rot, layer, offsetOverride, flipOverride);

        if (overHead)
        {
            context.AddOverHead(rot, draw);
        }
        else
        {
            context.AddBody(rot, draw);
        }
    }

    /// <summary>
    /// Turns a pawn render node's properties into a stand quad, mirroring what PawnRenderNodeWorker
    /// does with the same drawData. Pass an override when a node worker substitutes its own offset
    /// or flip.
    /// </summary>
    protected static OutfitStandDraw MakeDraw(OutfitStandDrawContext context, PawnRenderNodeProperties props, Graphic graphic, Rot4 rot, float layer, Vector3? offsetOverride = null, bool? flipOverride = null)
    {
        var drawData = props.drawData;

        var offset = offsetOverride ?? drawData?.OffsetForRot(rot) ?? Vector3.zero;
        if (offsetOverride == null && drawData is { scaleOffsetByBodySize: true })
        {
            offset *= context.BodySizeFactor;
        }

        var baseScale = drawData?.scale ?? 1f;
        if (context.IsChild && drawData != null)
        {
            baseScale *= drawData.childScale;
        }

        var scale = new Vector3(props.drawSize.x * baseScale, 1f, props.drawSize.y * baseScale);

        var flipMesh = flipOverride ?? drawData?.FlipForRot(rot) ?? false;
        var angle = drawData?.RotationOffsetForRot(rot) ?? 0f;
        if (flipMesh)
        {
            angle *= -1f;
        }

        return new OutfitStandDraw(graphic, layer, offset, scale, angle, flipMesh, false);
    }
}
