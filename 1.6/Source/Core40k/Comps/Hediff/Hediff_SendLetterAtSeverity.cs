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
            
        // LetterMaker assigns the unique load ID. Building the letter with an object initializer
        // leaves Letter.ID at 0, and two such letters in the archive collide on "Letter_0".
        var letter = LetterMaker.MakeLetter(Props.letter, Props.message, Props.letterDef, Pawn);

        Find.LetterStack.ReceiveLetter(letter);
        hasSentLetter = true;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref hasSentLetter, "hasSentLetter", false);
    }
}