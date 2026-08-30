using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

/// <summary>
/// Scales the grav engine's post-landing cooldown by the best crew member's
/// <see cref="Core40kDefOf.BEWH_GravEngineCooldownFactor"/>.
///
/// ConsumeFuel is the only place vanilla assigns cooldownCompleteTick, and it does so on its last
/// line, so a postfix can simply rescale what was just written. It runs from CompPilotConsole
/// before InitiateTakeoff, which means the crew is still spawned on the substructure and the scan
/// finds them.
///
/// Deliberately NOT patching GravshipUtility.LaunchCooldownFromQuality: it is static with no pawn
/// in scope, and the launch ritual dialog calls it to render the "won't be able to launch again
/// for X" preview - patching there would advertise a number the launch does not honour.
/// </summary>
[HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.ConsumeFuel))]
public static class GravEngineCooldownFromCrewPatch
{
    public static bool Prepare()
    {
        return ModsConfig.OdysseyActive;
    }

    public static void Postfix(Building_GravEngine __instance)
    {
        var factor = VoidfaringUtility.BestGravshipCrewStat(__instance, Core40kDefOf.BEWH_GravEngineCooldownFactor, 1f, true);
        if (Mathf.Approximately(factor, 1f))
        {
            return;
        }

        var now = GenTicks.TicksGame;
        var remaining = __instance.cooldownCompleteTick - now;
        if (remaining <= 0)
        {
            return;
        }

        __instance.cooldownCompleteTick = now + Mathf.Max(0, Mathf.RoundToInt(remaining * factor));
    }
}
