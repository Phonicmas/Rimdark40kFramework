using RimWorld;
using Verse;

namespace Core40k;

public class PawnRenderNodeWorker_HeadCovered : PawnRenderNodeWorker_FlipWhenCrawling
{
    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        var headCovered = parms.pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.FullHead) || parms.pawn.apparel.BodyPartGroupIsCovered(Core40kDefOf.UpperHead);

        return !headCovered && base.CanDrawNow(node, parms);
    }
}