using Verse;

namespace Core40k;

public class Hediff_RemoveMentalStateOnHediffEnd : HediffComp
{
    private HediffCompProperties_RemoveMentalStateOnHediffEnd Props => (HediffCompProperties_RemoveMentalStateOnHediffEnd)props;

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        if (!Pawn.InMentalState) return;
            
        if (Props.specificMentalState == null || Props.specificMentalState == Pawn.MentalStateDef)
        {
            Pawn.MentalState?.RecoverFromState();
        }
    }
}