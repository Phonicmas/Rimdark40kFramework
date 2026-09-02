using RimWorld;
using Verse;

namespace Core40k;

public class CompAbilityEffect_ResetRanks : CompAbilityEffect
{
    private new CompProperties_ResetRanks Props => (CompProperties_ResetRanks)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        target.Pawn?.GetComp<CompRankInfo>()?.ResetRanks(Props.rankCategoryDef);
    }
    
    private int CanDemoteTier()
    {
        if (!Props.ownRankAsTier)
        {
            return Props.canDemoteToTierInclusive;
        }

        var casterComp = parent.pawn?.GetComp<CompRankInfo>();
        return casterComp?.HighestRank() ?? Props.canDemoteToTierInclusive;
    }
        
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        base.Valid(target, throwMessages);
        
        var rankComp = target.Pawn?.GetComp<CompRankInfo>();
        if (rankComp == null || rankComp.UnlockedRanks.NullOrEmpty())
        {
            return false;
        }

        if (!rankComp.HasRankOfCategory(Props.rankCategoryDef))
        {
            return false;
        }

        return rankComp.HighestRank() <= CanDemoteTier();
    }
        
    public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
    {
        if (target.Pawn is not { } pawn)
        {
            return null;
        }

        var rankComp = pawn.GetComp<CompRankInfo>();
        if (rankComp == null)
        {
            return "BEWH.Framework.RankSystem.DoesNotHaveRank".Translate(pawn);
        }
            
        if (rankComp.UnlockedRanks.NullOrEmpty())
        {
            return "BEWH.Framework.RankSystem.NoUnlockedRanks".Translate(pawn);
        }

        if (!rankComp.HasRankOfCategory(Props.rankCategoryDef))
        {
            return "BEWH.Framework.RankSystem.NoUnlockedRanksOfCategory".Translate(pawn, Props.rankCategoryDef);
        }

        if (rankComp.HighestRank() > CanDemoteTier())
        {
            return "BEWH.Framework.RankSystem.RankTooHigh".Translate(pawn);
        }
            
        return null;
    }
}
