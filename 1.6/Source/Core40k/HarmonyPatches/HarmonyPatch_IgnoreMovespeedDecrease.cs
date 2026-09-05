using System.Collections.Concurrent;
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
            return IgnoreMovespeedDecreaseUtility.NewMoveSpeedOffsetFor(gear.def);
        }
        return val;
    }
}

public static class IgnoreMovespeedDecreaseUtility
{
    private static HashSet<GeneDef> negatingGeneDefs;

    private static readonly ConcurrentDictionary<ThingDef, float> newMoveSpeedOffsets = new();

    //Built on first use so the def database is complete, then shared by every MoveSpeed evaluation.
    private static HashSet<GeneDef> NegatingGeneDefs
    {
        get
        {
            if (negatingGeneDefs != null)
            {
                return negatingGeneDefs;
            }

            var result = new HashSet<GeneDef>();
            foreach (var geneDef in DefDatabase<GeneDef>.AllDefsListForReading)
            {
                if (geneDef.HasModExtension<DefModExtension_IgnoreMovespeedDecrease>())
                {
                    result.Add(geneDef);
                }
            }

            negatingGeneDefs = result;
            return result;
        }
    }

    public static float NewMoveSpeedOffsetFor(ThingDef gearDef)
    {
        return newMoveSpeedOffsets.GetOrAdd(gearDef, static def => def.GetModExtension<DefModExtension_IgnoreMovespeedDecrease>()?.newMoveSpeedOffset ?? 0f);
    }

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

        var negatingDefs = NegatingGeneDefs;
        if (negatingDefs.Count == 0)
        {
            return false;
        }

        var genes = pawnApparelTracker.pawn.genes.GenesListForReading;
        for (var i = 0; i < genes.Count; i++)
        {
            if (negatingDefs.Contains(genes[i].def))
            {
                negatingGene = genes[i];
                return true;
            }
        }

        return false;
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
