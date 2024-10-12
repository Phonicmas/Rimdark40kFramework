﻿using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k
{
    [HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
    public static class ApparelColourTwoPatch
    {
        public static bool Prefix(ref bool __result, Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec)
        {
            if (apparel is ApparelColourTwo)
            {
                __result = TryGetGraphicApparel(apparel, bodyType, ref rec);
                return false;
            }
            return true;
        }

        public static bool TryGetGraphicApparel(Apparel apparel, BodyTypeDef bodyType, ref ApparelGraphicRecord rec)
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
            string path = ((apparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead && apparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover && !apparel.RenderAsPack() && !(apparel.WornGraphicPath == BaseContent.PlaceholderImagePath) && !(apparel.WornGraphicPath == BaseContent.PlaceholderGearImagePath)) ? (apparel.WornGraphicPath + "_" + bodyType.defName) : apparel.WornGraphicPath);
            Shader shader = ShaderDatabase.Cutout;
            if (apparel.StyleDef?.graphicData.shaderType != null)
            {
                shader = apparel.StyleDef.graphicData.shaderType.Shader;
            }
            else if ((apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask))
            {
                shader = ShaderDatabase.CutoutComplex;
            }
            Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor, apparel.DrawColorTwo);
            rec = new ApparelGraphicRecord(graphic, apparel);
            return true;
        }
    }
}   