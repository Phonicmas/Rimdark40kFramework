using HarmonyLib;
using RimWorld;
using VEF.Apparels;
using Verse;
using ApparelLayerDefOf = RimWorld.ApparelLayerDefOf;

namespace Core40k;

[HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
public static class ApparelGraphicPatch
{
    public static bool Prefix(ref bool __result, Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec)
    {
        if (apparel.WornGraphicPath.NullOrEmpty())
        {
            return true;
        }

        if (!apparel.HasComp<CompMultiColor>() && !apparel.HasComp<CompAlternateTexture>())
        {
            return true;
        }
        
        //Either can be null
        var multiColor = apparel.GetComp<CompMultiColor>();
        var alternateTexture = apparel.GetComp<CompAlternateTexture>();

        if ((multiColor != null && multiColor.RecacheMultiGraphics) || (alternateTexture != null && alternateTexture.RecacheMultiGraphics))
        {
            var graphic = TryGetGraphicApparel(apparel, multiColor, alternateTexture, bodyType);
            
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
        
        __result = true;
        return false;
    }

    private static Graphic_Multi TryGetGraphicApparel(Apparel apparel, CompMultiColor multiColor, CompAlternateTexture alternateTexture, BodyTypeDef bodyType)
    {
        if (bodyType == null)
        {
            Log.Error("Getting apparel graphic with undefined body type.");
            bodyType = BodyTypeDefOf.Male;
        }

        bodyType = apparel.def.GetModExtension<DefModExtension_ForcesBodyType>()?.forcedBodyType ?? bodyType;
        var extension = apparel.def.GetModExtension<ApparelExtension>();

        var alternatePath = alternateTexture?.CurrentAlternateBaseForm?.drawnTextureIconPath;
        var usedPath = alternatePath.NullOrEmpty() ? apparel.WornGraphicPath : alternatePath;
        
        var path = apparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead 
                   && apparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover 
                   && !apparel.RenderAsPack() 
                   && usedPath != BaseContent.PlaceholderImagePath 
                   && usedPath != BaseContent.PlaceholderGearImagePath 
                   && extension is not { isUnifiedApparel: true }
                    ? usedPath + "_" + bodyType.defName : usedPath;
        
        //multiColor is explicitly allowed to be null here - apparel can carry CompAlternateTexture
        //on its own. apparel.def.graphicData.shaderType is also optional in XML.
        var shader = multiColor?.Props?.colorMaskAmount == 3
            ? Core40kDefOf.BEWH_CutoutThreeColor.Shader
            : apparel.def.graphicData?.shaderType?.Shader ?? ShaderDatabase.Cutout;
        var maskPath = multiColor?.MaskDef?.maskPath;
        var drawSize = alternateTexture?.CurrentAlternateBaseForm?.newDrawSize ?? apparel.def.graphicData.drawSize;
        if (multiColor?.MaskDef != null && multiColor.MaskDef.useBodyTypes)
        {
            maskPath += "_" + bodyType.defName;
        }
        var graphic = MultiColorUtils.GetGraphic<Graphic_Multi>(path, shader, drawSize, multiColor?.DrawColor ?? apparel.DrawColor, multiColor?.DrawColorTwo ?? apparel.DrawColorTwo, multiColor?.DrawColorThree ?? apparel.DrawColorTwo, null, maskPath);
        return graphic;
    }
}