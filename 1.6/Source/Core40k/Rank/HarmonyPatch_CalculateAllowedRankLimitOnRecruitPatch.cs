using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), "DoRecruit", [
    typeof(Pawn),
    typeof(Pawn),
    typeof(bool)
], [
    ArgumentType.Normal,
    ArgumentType.Normal,
    ArgumentType.Normal
])]
public class CalculateAllowedRankLimitOnRecruit
{
    public static void Postfix(Pawn recruiter, Pawn recruitee)
    {
        if (!recruitee.HasComp<CompRankInfo>() || recruitee.IsSlaveOfColony)
        {
            return;
        }

        var comp = recruitee.GetComp<CompRankInfo>();
        var gameComp = Current.Game.GetComponent<GameComponent_RankInfo>();
        var toRemove = new List<RankDef>();
        foreach (var rankDef in comp.UnlockedRanks)
        {
            if (gameComp.CanHaveMoreOfRank(rankDef))
            {
                gameComp.PawnGainedRank(rankDef);
            }
            else
            {
                toRemove.Add(rankDef);
            }
        }

        foreach (var rankDef in toRemove)
        {
            comp.RemoveRank(rankDef, false);
        }
            
    }
}