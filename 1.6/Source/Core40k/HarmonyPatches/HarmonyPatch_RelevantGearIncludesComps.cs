using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

//The character card's stat report was not crediting worn apparel for offsets that come from a comp
//rather than from the ThingDef.
//
//Vanilla decides what counts as relevant gear in two different ways:
//
//  RelevantGear(pawn, stat)                 apparel: GearAffectsStat(def, stat)
//                                           equipment: that, OR GearHasCompsThatAffectStat(thing, stat)
//  GetOffsetsAndFactorsExplanation(...)     same split again, inline
//
//and GearAffectsStat only looks at def.equippedStatOffsets. The comp escape hatch was only ever
//wired up for the weapon, presumably because CompBladelinkWeapon is the only vanilla case.
//
//The value itself was always right: GetValueUnfinalized walks WornApparel and calls
//StatOffsetFromGear, which GetOffsetsFromGearPatch postfixes with the comps' offsets. Only the
//explanation was missing the line, so a helmet granting +10.8 Move Speed through its decorations
//moved the pawn without ever saying why.
public static class RelevantGearUtility
{
    //What GearAffectsStat does, plus every CompGraphicParent on the item - the same comps
    //GetOffsetsFromGearPatch feeds into StatOffsetFromGear, so the two agree by construction.
    //Reimplemented from public members rather than reflecting into the private original.
    public static bool GearAffectsStatIncludingComps(Thing gear, StatDef stat)
    {
        if (gear?.def == null || stat == null)
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

//Makes the "Relevant gear" header appear at all. GetOffsetsAndFactorsExplanation gates the whole
//section on RelevantGear(pawn, stat).Any().
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

//Makes the apparel line itself appear. The loop over WornApparel filters on
//GearAffectsStat(apparel.def, stat), which cannot see the comps because it only ever gets the def.
//
//Both call sites in this method read as: <load gear>, ldfld Thing::def, ldarg.0, ldfld stat,
//call GearAffectsStat. Neutralising the ldfld leaves the gear itself on the stack, so the call can
//be retargeted at a check that takes the Thing and can therefore ask its comps. The equipment call
//site already had "|| GearHasCompsThatAffectStat", so widening it there is redundant but harmless
//and keeps both paths identical.
//
//The line's text is produced by vanilla InfoTextLineFromGear -> StatOffsetFromGear, which is
//already postfixed, so the number needs no work here.
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

                //Walk back to the ldfld that turned the gear into its def. Only the two loads that
                //push the StatDef sit in between, so a short window is enough.
                for (var j = i - 1; j >= 0 && j >= i - 5; j--)
                {
                    if (!codeInstructions[j].LoadsField(defField))
                    {
                        continue;
                    }

                    //Neutralise in place rather than removing: any labels or exception block
                    //boundaries attached to the instruction stay where they are.
                    codeInstructions[j].opcode = OpCodes.Nop;
                    codeInstructions[j].operand = null;
                    codeInstructions[i].operand = replacement;
                    patched++;
                    break;
                }
            }
        }

        //If another mod has already rewritten this method past recognition, leave it alone. The
        //cost is the old behaviour - a missing line in the report - never a broken stat card.
        if (patched == 0)
        {
            Log.Warning("[Core40k] Could not widen StatWorker.GetOffsetsAndFactorsExplanation to comp-sourced apparel offsets; the character card will not list apparel whose stat offsets come from decorations or upgrades. Everything else is unaffected.");
        }

        return codeInstructions;
    }
}
