using HarmonyLib;
using RimWorld;
using UnityEngine;
using VEF.Apparels;
using Verse;
using ApparelLayerDefOf = RimWorld.ApparelLayerDefOf;

namespace Core40k;

//The single prefix on TryGetGraphicApparel. This used to be two separate patch classes on the same
//method - one for multi colour / alternate texture apparel, one for apparel worn alongside
//something that forces a body type - and because Harmony stops at the first prefix that returns
//false, which of them won was decided by registration order rather than by anything meaningful.
//
//The two cases are mutually exclusive, so they are now two branches of one decision:
//  1. the framework paints this apparel itself (CompMultiColor / CompAlternateTexture), or
//  2. it is plain apparel being drawn on a body type another worn piece is forcing.
//Anything else, and vanilla handles it.
[HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
public static class ApparelGraphicPatch
{
    public static bool Prefix(ref bool __result, Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec)
    {
        if (apparel?.def == null || apparel.WornGraphicPath.NullOrEmpty())
        {
            return true;
        }

        //Either can be null; both being null means this is not our apparel to paint.
        var multiColor = apparel.GetComp<CompMultiColor>();
        var alternateTexture = apparel.GetComp<CompAlternateTexture>();

        if (multiColor != null || alternateTexture != null)
        {
            return PaintedApparelPrefix(ref __result, apparel, bodyType, ref rec, multiColor, alternateTexture);
        }

        return ForcedBodyTypePrefix(ref __result, apparel, ref rec);
    }

    //Returns true to let vanilla handle it, false when rec has been filled in.
    private static bool PaintedApparelPrefix(ref bool __result, Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec, CompMultiColor multiColor, CompAlternateTexture alternateTexture)
    {
        if ((multiColor != null && multiColor.RecacheMultiGraphics) || (alternateTexture != null && alternateTexture.RecacheMultiGraphics))
        {
            var graphic = TryGetGraphicApparel(apparel, multiColor, alternateTexture, bodyType);

            if (graphic == null)
            {
                //Never hand a null graphic to the render tree - let vanilla take this one.
                Log.ErrorOnce(
                    $"[Core40k] Could not build a graphic for {apparel.def.defName} (bodytype {bodyType?.defName ?? "null"}); falling back to vanilla.",
                    ("Core40kApparelGraphic" + apparel.def.defName).GetHashCode());
                return true;
            }

            if (multiColor != null)
            {
                multiColor.CachedGraphicMulti = graphic;
            }
            if (alternateTexture != null)
            {
                alternateTexture.CachedGraphicMulti = graphic;
            }
        }

        if (multiColor != null)
        {
            rec = multiColor.ApparelGraphicRecord;
        }
        else if (alternateTexture != null)
        {
            rec = alternateTexture.ApparelGraphicRecord;
        }

        if (rec.graphic == null)
        {
            return true;
        }

        __result = true;
        return false;
    }

    //Plain apparel on a pawn wearing something that forces a body type: vanilla would draw it on
    //the pawn's own body type, which does not match the forced body the rest of the outfit uses.
    private static bool ForcedBodyTypePrefix(ref bool __result, Apparel apparel, ref ApparelGraphicRecord rec)
    {
        //The piece doing the forcing renders through vanilla on its own terms.
        if (apparel.def.HasModExtension<DefModExtension_ForcesBodyType>())
        {
            return true;
        }

        if (apparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead)
        {
            return true;
        }

        var worn = apparel.Wearer?.apparel?.WornApparel;
        if (worn == null)
        {
            return true;
        }

        //One pass rather than the Any-then-First it used to do.
        DefModExtension_ForcesBodyType defMod = null;
        foreach (var wornApparel in worn)
        {
            defMod = wornApparel.def.GetModExtension<DefModExtension_ForcesBodyType>();
            if (defMod != null)
            {
                break;
            }
        }

        if (defMod?.forcedBodyType == null)
        {
            return true;
        }
        
        if (!TryGetForcedBodyTypeGraphic(apparel, defMod.forcedBodyType, false, out var recOut))
        {
            return true;
        }

        rec = recOut;
        __result = true;
        return false;
    }

    internal static Graphic_Multi TryGetGraphicApparel(Apparel apparel, CompMultiColor multiColor, CompAlternateTexture alternateTexture, BodyTypeDef bodyType, Vector2? drawSizeOverride = null)
    {
        if (bodyType == null)
        {
            Log.Error("Getting apparel graphic with undefined body type.");
            bodyType = BodyTypeDefOf.Male;
        }

        bodyType = apparel.def.GetModExtension<DefModExtension_ForcesBodyType>()?.forcedBodyType ?? bodyType;
        var extension = apparel.def.GetModExtension<ApparelExtension>();
        //Picks up "_Female" art where a modder supplied it, the same convention Female Apparel
        //Variants uses. Gender.None when unworn, which resolves to the plain bodytype texture.
        var gender = apparel.Wearer?.gender ?? Gender.None;

        var alternatePath = alternateTexture?.CurrentAlternateBaseForm?.drawnTextureIconPath;
        var usedPath = alternatePath.NullOrEmpty() ? apparel.WornGraphicPath : alternatePath;
        
        var useBodyType = apparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead
                          && apparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover
                          && !apparel.RenderAsPack()
                          && usedPath != BaseContent.PlaceholderImagePath
                          && usedPath != BaseContent.PlaceholderGearImagePath
                          && extension is not { isUnifiedApparel: true };

        var path = useBodyType ? BodyTypeUtils.BodyTypedPath(usedPath, bodyType, gender) : usedPath;
        
        var shader = multiColor?.Props?.colorMaskAmount == 3
            ? Core40kDefOf.BEWH_CutoutThreeColor.Shader
            : apparel.def.graphicData?.shaderType?.Shader ?? ShaderDatabase.Cutout;
        var maskPath = multiColor?.MaskDef?.maskPath;
        var drawSize = drawSizeOverride ?? alternateTexture?.CurrentAlternateBaseForm?.newDrawSize ?? apparel.def.graphicData.drawSize;
        if (multiColor?.MaskDef != null && multiColor.MaskDef.useBodyTypes)
        {
            maskPath = BodyTypeUtils.BodyTypedMaskPath(maskPath, bodyType, gender);
        }
        var graphic = MultiColorUtils.GetGraphic<Graphic_Multi>(path, shader, drawSize, multiColor?.DrawColor ?? apparel.DrawColor, multiColor?.DrawColorTwo ?? apparel.DrawColorTwo, multiColor?.DrawColorThree ?? apparel.DrawColorTwo, null, maskPath);
        return graphic;
    }

    //Vanilla's own graphic resolution, redone against a body type the pawn is not actually using.
    public static bool TryGetForcedBodyTypeGraphic(Apparel apparel, BodyTypeDef bodyType, bool forStatue, out ApparelGraphicRecord rec)
    {
        if (bodyType == null)
        {
            Log.Error("Getting apparel graphic with undefined body type.");
            bodyType = BodyTypeDefOf.Male;
        }
        if (apparel.WornGraphicPath.NullOrEmpty())
        {
            rec = new ApparelGraphicRecord(null, null);
            return false;
        }
        var useBodyType = apparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead
                          && apparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover
                          && !apparel.RenderAsPack()
                          && apparel.WornGraphicPath != BaseContent.PlaceholderImagePath
                          && apparel.WornGraphicPath != BaseContent.PlaceholderGearImagePath;

        var path = useBodyType
            ? BodyTypeUtils.BodyTypedPath(apparel.WornGraphicPath, bodyType, apparel.Wearer?.gender ?? Gender.None)
            : apparel.WornGraphicPath;
        var shader = ShaderDatabase.Cutout;
        if (!forStatue)
        {
            if (apparel.StyleDef?.graphicData.shaderType != null)
            {
                shader = apparel.StyleDef.graphicData.shaderType.Shader;
            }
            else if ((apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask))
            {
                shader = ShaderDatabase.CutoutComplex;
            }
        }
        var graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
        rec = new ApparelGraphicRecord(graphic, apparel);
        return true;
    }
}
