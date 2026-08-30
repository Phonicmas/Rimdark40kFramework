using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

[HarmonyPatch(typeof(Pawn_GuestTracker), "CapturedBy")]
public class CalculateAllowedRankLimitOnArrest
{
    public static void Postfix(Pawn ___pawn, Faction by)
    {
        if (by == null || !by.IsPlayer)
        {
            return;
        }
            
        if (!___pawn.HasComp<CompRankInfo>() || ___pawn.IsSlaveOfColony)
        {
            return;
        }

        var comp = ___pawn.GetComp<CompRankInfo>();
        Current.Game.GetComponent<GameComponent_RankInfo>().PawnResetRanks(comp.LimitCountedRanks.ToList());
        comp.LimitCountedRanks.Clear();
    }
}