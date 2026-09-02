using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public static class RelevantGearUtility
{
    public static bool GearAffectsStatIncludingComps(Thing gear, StatDef stat)
    {
        if (gear?.def == null || stat == null)
        {
            return false;
        }

        if (IgnoreMovespeedDecreaseUtility.HidesStatOffset(gear, stat))
        {
            return false;
        }

        if (!gear.def.equippedStatOffsets.NullOrEmpty())
        {
            foreach (var statModifier in gear.def.equippedStatOffsets)
            {
                if (statModifier.stat == stat && statModifier.value != 0f)
                {
                    return true;
                }
            }
        }

        if (gear is not ThingWithComps thingWithComps || thingWithComps.AllComps == null)
        {
            return false;
        }

        foreach (var comp in thingWithComps.AllComps)
        {
            if (comp is CompGraphicParent graphicComp && !Mathf.Approximately(graphicComp.GetStatOffset(stat), 0f))
            {
                return true;
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(StatWorker), "RelevantGear")]
public static class RelevantGearPatch
{
    public static void Postfix(ref IEnumerable<Thing> __result, Pawn pawn, StatDef stat)
    {
        __result = WithCompSourcedApparel(__result, pawn, stat);
    }

    private static IEnumerable<Thing> WithCompSourcedApparel(IEnumerable<Thing> original, Pawn pawn, StatDef stat)
    {
        var alreadyListed = new HashSet<Thing>();

        foreach (var gear in original)
        {
            if (IgnoreMovespeedDecreaseUtility.HidesStatOffset(gear, stat))
            {
                continue;
            }

            alreadyListed.Add(gear);
            yield return gear;
        }

        if (pawn?.apparel == null)
        {
            yield break;
        }

        foreach (var apparel in pawn.apparel.WornApparel)
        {
            if (!alreadyListed.Contains(apparel) && RelevantGearUtility.GearAffectsStatIncludingComps(apparel, stat))
            {
                yield return apparel;
            }
        }
    }
}

[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetOffsetsAndFactorsExplanation))]
public static class GearExplanationIncludesCompsPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codeInstructions = instructions.ToList();

        var gearAffectsStat = AccessTools.Method(typeof(StatWorker), "GearAffectsStat");
        var replacement = AccessTools.Method(typeof(RelevantGearUtility), nameof(RelevantGearUtility.GearAffectsStatIncludingComps));
        var defField = AccessTools.Field(typeof(Thing), nameof(Thing.def));

        var patched = 0;

        if (gearAffectsStat != null && replacement != null && defField != null)
        {
            for (var i = 0; i < codeInstructions.Count; i++)
            {
                if (!codeInstructions[i].Calls(gearAffectsStat))
                {
                    continue;
                }

                for (var j = i - 1; j >= 0 && j >= i - 5; j--)
                {
                    if (!codeInstructions[j].LoadsField(defField))
                    {
                        continue;
                    }

                    codeInstructions[j].opcode = OpCodes.Nop;
                    codeInstructions[j].operand = null;
                    codeInstructions[i].operand = replacement;
                    patched++;
                    break;
                }
            }
        }

        if (patched == 0)
        {
            Log.Warning("[Core40k] Could not widen StatWorker.GetOffsetsAndFactorsExplanation to comp-sourced apparel offsets; the character card will not list apparel whose stat offsets come from decorations or upgrades. Everything else is unaffected.");
        }

        return codeInstructions;
    }
}
