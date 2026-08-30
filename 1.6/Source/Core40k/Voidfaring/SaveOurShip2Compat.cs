using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

/// <summary>
/// Optional integration with Save Our Ship 2 (2.8+). Nothing here is referenced unless SoS2 is
/// loaded, and every lookup is by name, so the framework carries no dependency on it.
///
/// Both hooks add effective skill levels to SoS2's own crew terms rather than multiplying the
/// final chance. That keeps the bonus inside SoS2's tuning curves, respects the player's
/// dodgeSkillImpact setting, and cannot produce a value SoS2 itself would never generate.
///
/// SoS2 side, for reference (SaveOurShip2.AccuracyCalculator):
///   ThisMapEvasionBoost    - highest Intellectual among pawns manning a ship bridge on the
///                            defending map, +2 for an Odyssey PilotAssistant, floor 10 with an
///                            AI core. Feeds DodgeChanceMultiplierFromPiloting: 0 -> 0.5x,
///                            20 -> 1.5x, 22 -> 1.55x. Nothing above 22 does anything.
///   SourceMapAccuracyBoost - the attacking map's tactical console boost. Feeds
///                            DodgeChanceMultiplierFromShooting: 0 -> 1.5x target dodge,
///                            20 -> 0.5x. Also drives projectile miss angle.
/// </summary>
public static class SaveOurShip2Compat
{
    private const string AccuracyCalculatorTypeName = "SaveOurShip2.AccuracyCalculator";
    private const string EvasionPropertyName = "ThisMapEvasionBoost";
    private const string GunneryPropertyName = "SourceMapAccuracyBoost";

    public static bool Active { get; private set; }

    /// <summary>
    /// Called from Core40kMod's constructor, after PatchAll. Attribute-driven patches cannot be
    /// used here - PatchAll resolves their targets eagerly and would throw when SoS2 is absent.
    /// </summary>
    public static void Apply(Harmony harmony)
    {
        // The type resolving at all is the check: if SoS2 is not loaded, it is not there.
        var accuracyCalculator = AccessTools.TypeByName(AccuracyCalculatorTypeName);
        if (accuracyCalculator == null)
        {
            return;
        }

        var patched = 0;
        patched += TryPatchGetter(harmony, accuracyCalculator, EvasionPropertyName, nameof(EvasionPostfix)) ? 1 : 0;
        patched += TryPatchGetter(harmony, accuracyCalculator, GunneryPropertyName, nameof(GunneryPostfix)) ? 1 : 0;

        Active = patched > 0;

        if (patched < 2)
        {
            Log.Warning($"[RimDark Framework] Save Our Ship 2 is present but only {patched}/2 voidfaring hooks applied. Void evasion and/or void gunnery will do nothing. This usually means SoS2 changed its accuracy code.");
        }
    }

    private static bool TryPatchGetter(Harmony harmony, Type type, string propertyName, string postfixName)
    {
        try
        {
            var getter = AccessTools.PropertyGetter(type, propertyName);
            if (getter == null)
            {
                Log.Warning($"[RimDark Framework] Could not find {type.FullName}.{propertyName} - skipping that Save Our Ship 2 hook.");
                return false;
            }

            harmony.Patch(getter, postfix: new HarmonyMethod(AccessTools.Method(typeof(SaveOurShip2Compat), postfixName)));
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning($"[RimDark Framework] Failed to patch {type.FullName}.{propertyName} for Save Our Ship 2 integration: {ex.Message}");
            return false;
        }
    }

    // AccuracyCalculator holds the defending and attacking maps in private fields named thisMap
    // and sourceMap. Harmony injects them by name, so neither postfix has to name a SoS2 type.

    public static void EvasionPostfix(Map ___thisMap, ref int __result)
    {
        __result += CrewOffset(___thisMap, Core40kDefOf.BEWH_ShipEvasionSkillOffset);
    }

    public static void GunneryPostfix(Map ___sourceMap, ref int __result)
    {
        __result += CrewOffset(___sourceMap, Core40kDefOf.BEWH_ShipGunnerySkillOffset);
    }

    private static int CrewOffset(Map map, StatDef stat)
    {
        if (map == null)
        {
            return 0;
        }

        var offset = VoidfaringUtility.BestMapCrewStat(map, stat, 0f, false);
        return offset <= 0f ? 0 : Mathf.RoundToInt(offset);
    }
}
