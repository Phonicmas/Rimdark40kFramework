using HarmonyLib;
using RimWorld;
using VEF.Apparels;
using Verse;
using ApparelLayerDefOf = RimWorld.ApparelLayerDefOf;

namespace Core40k;

[HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
public static class ApparelMultiColorPatch
{
    public static bool Prefix(ref bool __result, Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec)
    {
        if (apparel.WornGraphicPath.NullOrEmpty())
        {
            return true;
        }

        if (!apparel.HasComp<CompMultiColor>())
        {
            return true;
        }
        
        var multiColor = apparel.GetComp<CompMultiColor>();

        if (multiColor.RecacheMultiGraphics)
        {
            var graphic = TryGetGraphicApparel(apparel, multiColor, bodyType);
            multiColor.CachedGraphicMulti = graphic;
        }

        rec = multiColor.ApparelGraphicRecord;
        __result = true;
        return false;
    }

    private static Graphic_Multi TryGetGraphicApparel(Apparel apparel, CompMultiColor multiColor, BodyTypeDef bodyType)
    {
        if (bodyType == null)
        {
            Log.Error("Getting apparel graphic with undefined body type.");
            bodyType = BodyTypeDefOf.Male;
        }

        bodyType = apparel.def.GetModExtension<DefModExtension_ForcesBodyType>()?.forcedBodyType ?? bodyType;
        var extension = apparel.def.GetModExtension<ApparelExtension>();

        var alternatePath = multiColor.CurrentAlternateBaseForm?.drawnTextureIconPath;
        var usedPath = alternatePath.NullOrEmpty() ? apparel.WornGraphicPath : alternatePath;
        
        var path = apparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead 
                   && apparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover 
                   && !apparel.RenderAsPack() 
                   && usedPath != BaseContent.PlaceholderImagePath 
                   && usedPath != BaseContent.PlaceholderGearImagePath 
                   && extension is not { isUnifiedApparel: true }
                    ? usedPath + "_" + bodyType.defName : usedPath;
        
        var shader = Core40kDefOf.BEWH_CutoutThreeColor.Shader;
        var maskPath = multiColor.MaskDef?.maskPath;
        var drawSize = multiColor.CurrentAlternateBaseForm?.newDrawSize ?? apparel.def.graphicData.drawSize;
        if (multiColor.MaskDef != null && multiColor.MaskDef.useBodyTypes)
        {
            maskPath += "_" + bodyType.defName;
        }
        var graphic = MultiColorUtils.GetGraphic<Graphic_Multi>(path, shader, drawSize, multiColor.DrawColor, multiColor.DrawColorTwo, multiColor.DrawColorThree, null, maskPath);
        return graphic;
    }
}