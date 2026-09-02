using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Building_OutfitStand), "RecacheGraphics")]
public static class OutfitStandRecacheDecorations
{
    public static void Postfix(Building_OutfitStand __instance)
    {
        OutfitStandDecorationRenderer.Invalidate(__instance);
    }
}

[HarmonyPatch(typeof(Building_OutfitStand), "DrawAt")]
public static class OutfitStandDrawDecorations
{
    public static void Postfix(Building_OutfitStand __instance, Vector3 drawLoc, bool flip)
    {
        OutfitStandDecorationRenderer.Draw(__instance, drawLoc, flip ? __instance.Rotation.Opposite : __instance.Rotation);
    }
}
