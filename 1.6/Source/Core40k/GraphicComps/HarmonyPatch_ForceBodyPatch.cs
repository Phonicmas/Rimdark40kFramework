using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
public static class ForceBodyPatch
{
    public static bool Prefix(ref bool __result, Apparel apparel, ref ApparelGraphicRecord rec)
    {
        if (apparel.def.HasModExtension<DefModExtension_ForcesBodyType>())
        {
            return true;
        }

        if (apparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead )
        {
            return true;
        }

        var pawn = apparel.Wearer;

        if (pawn == null)
        {
            return true;
        }

        if (!Enumerable.Any(pawn.apparel.WornApparel, apparelInner => apparelInner.def.HasModExtension<DefModExtension_ForcesBodyType>()))
        {
            return true;
        }

        var defMod = pawn.apparel.WornApparel
            .First(apparelInner => apparelInner.def.HasModExtension<DefModExtension_ForcesBodyType>()).def
            .GetModExtension<DefModExtension_ForcesBodyType>();

        TryGetGraphicApparel(apparel, defMod.forcedBodyType, false, out var recOut);
        rec = recOut;
        __result = true;
        return false;
    }
    
    public static bool TryGetGraphicApparel(Apparel apparel, BodyTypeDef bodyType, bool forStatue, out ApparelGraphicRecord rec)
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