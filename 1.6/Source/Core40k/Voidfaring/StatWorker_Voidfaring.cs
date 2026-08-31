using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Visibility worker for the stats in the BEWH_Voidfaring category.
///
/// Vanilla StatWorker.ShouldShowFor is a hardcoded chain of StatCategoryDefOf comparisons. A
/// mod-added category falls off the end of it into Log.Error("Unhandled case: ..."), which fires
/// once per stat every time any info card is opened. The only escape vanilla offers is
/// StatCategoryDef.displayAllByDefault, which BEWH_Voidfaring now sets - but that alone would put
/// these stats on walls, weapons and terrain as well.
///
/// So: let the base call take the displayAllByDefault path instead of erroring, then narrow the
/// result back down to pawns. Deferring to base keeps every vanilla filter intact (alwaysHide,
/// showIfUndefined, showOnAnimals, showOnDrones, showOnMechanoids, hediff and developmental-stage
/// filters), so the flags set on the StatDefs themselves keep working unchanged.
/// </summary>
public class StatWorker_Voidfaring : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        return req.Def is ThingDef { category: ThingCategory.Pawn } && base.ShouldShowFor(req);
    }
}
