using RimWorld;
using Verse;

namespace Core40k;

public class Hediff_SendLetterAtSeverity : HediffComp
{
    protected const int SeverityUpdateInterval = 500;

    private bool hasSentLetter = false;

    private HediffCompProperties_SendLetterAtSeverity Props => (HediffCompProperties_SendLetterAtSeverity)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        if (!Pawn.IsHashIntervalTick(SeverityUpdateInterval))
        {
            return;
        }

        if (!(parent.Severity >= Props.severitySendAt))
        {
            return;
        }
            
        if (Props.onlySendOnce && hasSentLetter)
        {
            return;
        }

        if (Props.onlyForPlayerPawns && !IsPlayerRelevant(Pawn))
        {
            return;
        }

        var letterDef = Props.letterDef ?? LetterDefOf.NeutralEvent;
        var letter = LetterMaker.MakeLetter(Props.letter, Props.message, letterDef, Pawn);

        Find.LetterStack.ReceiveLetter(letter);
        hasSentLetter = true;
    }

    private static bool IsPlayerRelevant(Pawn pawn)
    {
        if (pawn == null || !pawn.Spawned)
        {
            return false;
        }

        return pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref hasSentLetter, "hasSentLetter", false);
    }
}