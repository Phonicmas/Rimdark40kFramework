using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Pawn_SkillTracker), "Learn")]
public class RecreationFromSkill
{
    public static void Postfix(SkillDef sDef, float xp, Pawn ___pawn)
    {
        var rankComp = ___pawn.GetComp<CompRankInfo>();
        if (rankComp == null)
        {
            return;
        }
        if (xp > 0f && rankComp.RecreationSkillsFromRanks.Contains(sDef))
        {
            ___pawn.needs?.joy?.GainJoy(xp * 0.001f, Core40kDefOf.BEWH_RecreationFromSkill);
        }
    }
}