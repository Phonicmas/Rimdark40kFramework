using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", [
    typeof(Pawn),
    typeof(SkillDef),
    typeof(bool)
], [
    ArgumentType.Normal,
    ArgumentType.Normal,
    ArgumentType.Normal
])]
public class QualityCreatedBoostFromGene
{
    public static void Postfix(Pawn pawn, SkillDef relevantSkill, ref QualityCategory __result )
    {
        if (pawn.genes == null)
        {
            return;
        }

        if (relevantSkill == null)
        {
            return;
        }

        var levelIncrease = 0;
        foreach (var gene in pawn.genes.GenesListForReading)
        {
            var defMod = gene.def?.GetModExtension<DefModExtension_BoostQualityCreatedByPawn>();
            if (defMod?.qualityBoostLevel == null)
            {
                continue;
            }

            if (defMod.qualityBoostLevel.TryGetValue(relevantSkill, out var boost))
            {
                levelIncrease += boost;
            }
        }

        if (levelIncrease == 0)
        {
            return;
        }

        __result = (QualityCategory)Mathf.Min((int)__result + levelIncrease, 6);
    }
}