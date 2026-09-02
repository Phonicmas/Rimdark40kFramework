using Verse;

namespace Core40k;

public class HediffCompProperties_SendLetterAtSeverity : HediffCompProperties
{
    public bool onlySendOnce = true;

    public float severitySendAt = 1f;

    public string letter = "";

    public string message = "";

    public LetterDef letterDef = null;

    public bool onlyForPlayerPawns = true;

    public HediffCompProperties_SendLetterAtSeverity()
    {
        compClass = typeof(Hediff_SendLetterAtSeverity);
    }
}