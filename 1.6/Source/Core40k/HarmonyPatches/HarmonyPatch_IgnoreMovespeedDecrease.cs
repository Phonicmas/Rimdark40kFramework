using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(StatWorker), "StatOffsetFromGear")]
public static class StatWorker_StatOffsetFromGear_Patch
{
    public static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
    {
        var codes = codeInstructions.ToList();

        var injectAt = -1;

        for (var i = 0; i < codes.Count && injectAt < 0; i++)
        {
            if (!CallsGetStatOffsetFromList(codes[i]))
            {
                continue;
            }

            for (var j = i + 1; j < codes.Count; j++)
            {
                if (codes[j].opcode != OpCodes.Stloc_0)
                {
                    continue;
                }

                injectAt = j;
                break;
            }
        }

        if (injectAt < 0)
        {
            injectAt = codes.FindIndex(code => code.opcode == OpCodes.Stloc_0);
            Log.Warning("[Core40k] Could not anchor the StatOffsetFromGear patch on GetStatOffsetFromList; falling back to the first local store.");
        }

        if (injectAt < 0)
        {
            Log.Error("[Core40k] Could not patch StatWorker.StatOffsetFromGear at all; move speed negation genes will not work.");
            return codes;
        }

        codes.InsertRange(injectAt + 1,
        [
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StatWorker_StatOffsetFromGear_Patch), nameof(ChangeValueIfNeeded))),
            new CodeInstruction(OpCodes.Stloc_0),
        ]);

        return codes;
    }

    private static bool CallsGetStatOffsetFromList(CodeInstruction code)
    {
        if (code.opcode != OpCodes.Call && code.opcode != OpCodes.Callvirt)
        {
            return false;
        }

        return code.operand is MethodInfo method
               && method.DeclaringType == typeof(StatUtility)
               && method.Name == "GetStatOffsetFromList";
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
    
    public static bool HidesStatOffset(Thing gear, StatDef stat)
    {
        return TryGetNegatingGene(gear, stat, out _) && Mathf.Approximately(StatWorker.StatOffsetFromGear(gear, stat), 0f);
    }
}

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
