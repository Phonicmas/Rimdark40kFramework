using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(ResurrectionUtility), "TryResurrect")]
public class RankLimitResurrectionReAddPatch
{
    public static void Postfix(ref bool __result, Pawn pawn)
    {
        if (!__result || !pawn.HasComp<CompRankInfo>())
        {
            return;
        }

        var comp = pawn.GetComp<CompRankInfo>();
        var gameComp = Current.Game.GetComponent<GameComponent_RankInfo>();

        foreach (var rank in comp.UnlockedRanks.ToList().Where(RankUtils.IsLimited))
        {
            if (comp.LimitCountedRanks.Contains(rank))
            {
                continue;
            }

            if (gameComp.CanHaveMoreOfRank(rank))
            {
                gameComp.PawnGainedRank(rank);
                comp.LimitCountedRanks.Add(rank);
            }
            else
            {
                comp.RemoveRank(rank, false);
            }
        }
    }
}