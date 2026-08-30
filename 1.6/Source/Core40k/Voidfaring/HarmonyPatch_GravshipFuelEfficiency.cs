using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

/// <summary>
/// Adds the best crew member's <see cref="Core40kDefOf.BEWH_GravshipFuelEfficiency"/> to the
/// gravship's own fuel savings.
///
/// FuelSavingsPercent is the right hook rather than FuelUseageFactor or FuelPerTile: those two
/// are one-liners that Mono is free to inline, while this one walks the component list and so
/// always survives as a real call. Everything downstream reads through it -
/// FuelUseageFactor => 1 - FuelSavingsPercent, FuelPerTile => 10 * FuelUseageFactor - including
/// the engine's own "gravship fuel consumption" stat card entry, which therefore reports the
/// crew bonus for free.
/// </summary>
[HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.FuelSavingsPercent), MethodType.Getter)]
public static class GravshipFuelSavingsFromCrewPatch
{
    // Total savings ceiling. Without it, enough stacked savings makes travel free or negative.
    private const float MaxTotalSavings = 0.9f;

    public static bool Prepare()
    {
        return ModsConfig.OdysseyActive;
    }

    public static void Postfix(Building_GravEngine __instance, ref float __result)
    {
        var crewSavings = VoidfaringUtility.BestGravshipCrewStat(__instance, Core40kDefOf.BEWH_GravshipFuelEfficiency, 0f, false);
        if (crewSavings <= 0f)
        {
            return;
        }

        __result = Mathf.Min(MaxTotalSavings, __result + crewSavings);
    }
}
