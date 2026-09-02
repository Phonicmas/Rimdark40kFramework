using RimWorld;
using Verse;

namespace Core40k;

public class CompAbilityEffect_GiveHediffAndMentalBreak : CompAbilityEffect_GiveHediff
{
    public CompProperties_AbilityGiveHediffAndMental PropsMental => (CompProperties_AbilityGiveHediffAndMental)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        
        if (PropsMental.mentalStateDef == null || target.Pawn?.mindState?.mentalStateHandler == null)
        {
            return;
        }

        target.Pawn.mindState.mentalStateHandler.TryStartMentalState(PropsMental.mentalStateDef);
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        return target.Pawn != null && base.Valid(target, throwMessages);
    }
}