using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

public static class RankUtils
{
    private static Dictionary<RankDef, RankCategoryDef> categoryOfRank;

    private const int MaxPrerequisiteDepth = 20;

    public static RankCategoryDef CategoryOf(RankDef rank)
    {
        if (categoryOfRank == null)
        {
            categoryOfRank = new Dictionary<RankDef, RankCategoryDef>();
            foreach (var category in DefDatabase<RankCategoryDef>.AllDefsListForReading)
            {
                foreach (var data in category.ranks.Where(data => data.rankDef != null))
                {
                    if (!categoryOfRank.ContainsKey(data.rankDef))
                    {
                        categoryOfRank.Add(data.rankDef, category);
                    }
                }
            }
        }

        return categoryOfRank.TryGetValue(rank, out var found) ? found : null;
    }

    public static bool IsLimited(RankDef rank)
    {
        return rank.colonyLimitOfRank.x > 0 || (rank.colonyLimitOfRank.x == 0 && rank.colonyLimitOfRank.y > 0);
    }

    public static void TryGivePawnKindRanks(Pawn pawn)
    {
        if (pawn?.kindDef == null || Current.Game == null)
        {
            return;
        }

        var ext = pawn.kindDef.GetModExtension<DefModExtension_PawnKindRanks>();
        if (ext == null || ext.ranks.NullOrEmpty())
        {
            return;
        }

        var comp = pawn.GetComp<CompRankInfo>();
        if (comp == null)
        {
            return;
        }

        //Only player faction pawns occupy limited rank slots on generation.
        //Recruited and captured pawns are counted by their own patches.
        var countTowardsLimit = pawn.Faction is { IsPlayer: true };

        var planned = new List<RankDef>();
        var daysPerRank = new Dictionary<RankDef, float>();

        foreach (var entry in ext.ranks)
        {
            if (entry.chance < 1f && !Rand.Chance(entry.chance))
            {
                continue;
            }

            var rank = entry.ResolveRank();
            if (rank == null)
            {
                continue;
            }

            var category = entry.rankCategory ?? CategoryOf(rank);
            if (category == null)
            {
                Log.WarningOnce(pawn.kindDef.defName + " wants to grant rank " + rank.defName + " but it is not part of any RankCategoryDef.", pawn.kindDef.shortHash ^ rank.shortHash);
                continue;
            }

            if (entry.requireCategoryUnlocked && !category.RankCategoryUnlockedFor(pawn))
            {
                continue;
            }

            var collected = new List<RankDef>();
            CollectRankWithPrerequisites(rank, category, entry.includePrerequisites, collected, 0);

            foreach (var collectedRank in collected)
            {
                planned.Add(collectedRank);

                if (entry.daysAsRank <= 0f)
                {
                    continue;
                }

                var existing = daysPerRank.TryGetValue(collectedRank, out var days) ? days : 0f;
                daysPerRank[collectedRank] = Math.Max(existing, entry.daysAsRank);
            }
        }

        foreach (var rank in planned.Distinct().OrderBy(rank => rank.rankTier))
        {
            if (comp.HasRank(rank))
            {
                continue;
            }

            if (!rank.incompatibleRanks.NullOrEmpty() && rank.incompatibleRanks.Any(comp.HasRank))
            {
                continue;
            }

            if (countTowardsLimit && IsLimited(rank) && !comp.GameComponentRankInfo.CanHaveMoreOfRank(rank))
            {
                continue;
            }

            comp.UnlockRank(rank, countTowardsLimit);

            if (daysPerRank.TryGetValue(rank, out var daysAsRank) && daysAsRank > 0f)
            {
                comp.SetDaysAsRank(rank, daysAsRank);
            }
        }
    }

    private static void CollectRankWithPrerequisites(RankDef rank, RankCategoryDef category, bool includePrerequisites, List<RankDef> into, int depth)
    {
        if (rank == null || into.Contains(rank) || depth > MaxPrerequisiteDepth)
        {
            return;
        }

        if (includePrerequisites && category.rankDict.TryGetValue(rank, out var data))
        {
            if (!data.rankRequirements.NullOrEmpty())
            {
                foreach (var required in data.rankRequirements.Where(required => required.rankDef != null))
                {
                    CollectRankWithPrerequisites(required.rankDef, category, true, into, depth + 1);
                }
            }

            if (!data.rankRequirementsOneAmong.NullOrEmpty())
            {
                var cheapest = data.rankRequirementsOneAmong
                    .Where(required => required.rankDef != null)
                    .OrderBy(required => required.rankDef.rankTier)
                    .ThenBy(required => required.daysAs)
                    .FirstOrDefault();

                if (cheapest != null)
                {
                    CollectRankWithPrerequisites(cheapest.rankDef, category, true, into, depth + 1);
                }
            }
        }

        into.Add(rank);
    }
}
