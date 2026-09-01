using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

//Thanks VE Team for letting theirs as a base!
[HarmonyPatch(typeof(StatWorker), "StatOffsetFromGear")]
public static class StatWorker_StatOffsetFromGear_Patch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
    {
        var patched = false;
        var codes = codeInstructions.ToList();
        foreach (var code in codes)
        {
            yield return code;
            if (patched || code.opcode != OpCodes.Stloc_0)
            {
                continue;
            }

            yield return new CodeInstruction(OpCodes.Ldloc_0);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StatWorker_StatOffsetFromGear_Patch), "ChangeValueIfNeeded"));
            yield return new CodeInstruction(OpCodes.Stloc_0);
            patched = true;
        }
    }

    public static float ChangeValueIfNeeded(float val, Thing gear, StatDef stat)
    {
        if (stat == StatDefOf.MoveSpeed && val < 0f && IgnoreMovespeedDecreaseUtility.TryGetNegatingGene(gear, stat, out _))
        {
            var defMod = gear.def.GetModExtension<DefModExtension_IgnoreMovespeedDecrease>();
            return defMod?.newMoveSpeedOffset ?? 0f;
        }
        return val;
    }
}

public static class IgnoreMovespeedDecreaseUtility
{
    //Shared by the numeric override above and by every place that decides whether the gear gets a
    //line on a stat card, so the two can never disagree about what is being negated.
    //Mirrors the check StatWorker.StatOffsetFromGear itself starts from (the def's own
    //equippedStatOffsets) rather than re-deriving the final value, which by then has already been
    //zeroed by our own transpiler.
    public static bool TryGetNegatingGene(Thing gear, StatDef stat, out Gene negatingGene)
    {
        negatingGene = null;
        if (stat != StatDefOf.MoveSpeed || gear?.def?.equippedStatOffsets == null)
        {
            return false;
        }

        if (gear.ParentHolder is not Pawn_ApparelTracker pawnApparelTracker || pawnApparelTracker.pawn.genes == null)
        {
            return false;
        }

        if (StatUtility.GetStatOffsetFromList(gear.def.equippedStatOffsets, stat) >= 0f)
        {
            return false;
        }

        negatingGene = pawnApparelTracker.pawn.genes.GenesListForReading
            .FirstOrDefault(gene => gene.def.HasModExtension<DefModExtension_IgnoreMovespeedDecrease>());
        return negatingGene != null;
    }

    //The gene cancelled this gear's move speed penalty and nothing else on the item puts a value
    //back, so there is nothing honest left to report: the item should not appear under Move Speed
    //on either stat card at all, not even as a "+0" line.
    //
    //Asking StatOffsetFromGear for the final number (rather than assuming zero) keeps a decoration
    //or upgrade that contributes its own offset visible - GetOffsetFromGearPatch adds those after
    //the negation, so such an item still has something true to say.
    public static bool HidesStatOffset(Thing gear, StatDef stat)
    {
        return TryGetNegatingGene(gear, stat, out _) && Mathf.Approximately(StatWorker.StatOffsetFromGear(gear, stat), 0f);
    }
}

//The equipment's own info card. ThingDef.SpecialDisplayStats emits one EquippedStatOffsets row per
//entry in equippedStatOffsets; when the request carries the actual Thing it re-reads the value
//through the already-patched StatOffsetFromGear, so the row reads "+0" instead of "-0.2". The row
//is emitted unconditionally though, so it has to be filtered out here.
//
//Gated on req.HasThing, and TryGetNegatingGene additionally requires the item to be worn, so the
//def-only card - a hyperlink, or the piece lying on the ground - still shows the honest penalty it
//imposes on anyone without the gene.
[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats), typeof(StatRequest))]
public static class SpecialDisplayStatsHidesNegatedMovespeedPatch
{
    public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> values, StatRequest req)
    {
        foreach (var statDrawEntry in values)
        {
            if (statDrawEntry != null && req.HasThing && statDrawEntry.category == StatCategoryDefOf.EquippedStatOffsets && IgnoreMovespeedDecreaseUtility.HidesStatOffset(req.Thing, statDrawEntry.stat))
            {
                continue;
            }

            yield return statDrawEntry;
        }
    }
}
