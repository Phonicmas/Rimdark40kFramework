using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Core40k;

public static class RankEligibilityNotifier
{
    //Every colonist is checked once within this many ticks.
    private const int SweepIntervalTicks = 1250;

    //The sweep is broken into steps so a large colony never spikes on a single tick.
    private const int SweepSteps = 25;

    private const int StepIntervalTicks = SweepIntervalTicks / SweepSteps;

    private static int sweepIndex;

    public static void Tick()
    {
        if (!Core40kUtils.ModSettings.notifyOnRankEligibility)
        {
            return;
        }

        if (Find.TickManager.TicksGame % StepIntervalTicks != 0)
        {
            return;
        }

        var colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists_NoCryptosleep;
        if (colonists.NullOrEmpty())
        {
            sweepIndex = 0;
            return;
        }

        var pawnsThisStep = (colonists.Count + SweepSteps - 1) / SweepSteps;

        for (var i = 0; i < pawnsThisStep; i++)
        {
            if (sweepIndex >= colonists.Count)
            {
                sweepIndex = 0;
            }

            CheckPawn(colonists[sweepIndex], true);
            sweepIndex++;
        }
    }
    
    public static void SeedBaseline()
    {
        foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
        {
            CheckPawn(pawn, false);
        }
    }

    private static void CheckPawn(Pawn pawn, bool announce)
    {
        if (pawn?.story == null || pawn.skills == null || pawn.Faction is not { IsPlayer: true })
        {
            return;
        }

        var comp = pawn.GetComp<CompRankInfo>();
        if (comp == null)
        {
            return;
        }

        List<RankDef> newlyEligible = null;

        foreach (var category in DefDatabase<RankCategoryDef>.AllDefsListForReading)
        {
            if (!category.RankCategoryUnlockedFor(pawn))
            {
                continue;
            }

            foreach (var data in category.ranks)
            {
                var rank = data?.rankDef;
                if (rank == null || comp.HasRank(rank) || comp.HasAnnouncedEligibility(rank))
                {
                    continue;
                }

                if (!rank.RequirementMet(pawn, comp, category))
                {
                    continue;
                }

                comp.MarkEligibilityAnnounced(rank);

                newlyEligible ??= [];
                newlyEligible.Add(rank);
            }
        }

        if (!announce || newlyEligible.NullOrEmpty())
        {
            return;
        }

        var rankList = newlyEligible
            .OrderBy(rank => rank.rankTier)
            .Select(rank => rank.label.CapitalizeFirst())
            .ToCommaList(useAnd: true);

        var text = newlyEligible.Count == 1
            ? "BEWH.Framework.RankSystem.EligibleForRank".Translate(pawn.LabelShortCap, rankList)
            : "BEWH.Framework.RankSystem.EligibleForRanks".Translate(pawn.LabelShortCap, rankList);

        Messages.Message(text.Resolve(), new LookTargets(pawn), MessageTypeDefOf.PositiveEvent);
    }
}
