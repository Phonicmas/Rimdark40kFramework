using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

public class PawnKindRankData
{
    //Fixed rank to grant. Ignored when rankOptions is used.
    public RankDef rank;

    //Weighted random pick, used when rank is null.
    public List<PawnKindRankOption> rankOptions = [];

    //Category used to resolve prerequisites and unlock conditions.
    //Leave null to auto detect the first RankCategoryDef containing the rank.
    public RankCategoryDef rankCategory;

    //Also grant everything the rank requires, recursively.
    public bool includePrerequisites = true;

    //0-1 chance that this entry is applied at all.
    public float chance = 1f;

    //Pretend the pawn has held the granted ranks for this many days,
    //so time gated follow up ranks are reachable right away.
    public float daysAsRank = 0f;

    //Skip the entry when the pawn does not fulfill the rank categories unlock conditions.
    public bool requireCategoryUnlocked = true;

    public RankDef ResolveRank()
    {
        if (rank != null)
        {
            return rank;
        }

        if (rankOptions.NullOrEmpty())
        {
            return null;
        }

        var selection = new WeightedSelection<RankDef>();
        foreach (var option in rankOptions.Where(option => option.rank != null && option.weight > 0f))
        {
            selection.AddEntry(option.rank, option.weight);
        }

        return selection.NoEntriesOrNull() ? null : selection.GetRandom();
    }
}

public class PawnKindRankOption
{
    public RankDef rank;

    public float weight = 1f;
}
