using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Adds the best crew member's <see cref="Core40kDefOf.BEWH_GravshipRangeOffset"/> to the vanilla
/// GravshipRange stat. Patched onto Ludeon's own StatDef rather than hooking
/// Building_GravEngine.MaxLaunchDistance, so the bonus shows up in the engine's stat report with
/// a named line instead of appearing as an unexplained number.
/// </summary>
public class StatPart_GravshipCrewRange : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        var offset = OffsetFor(req);
        if (offset != 0f)
        {
            val += offset;
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        var offset = OffsetFor(req);
        if (offset == 0f)
        {
            return null;
        }

        return "BEWH.Framework.Voidfaring.CrewRangeOffset".Translate() + ": " + offset.ToStringWithSign("F0");
    }

    private static float OffsetFor(StatRequest req)
    {
        if (!req.HasThing || req.Thing is not Building_GravEngine engine)
        {
            return 0f;
        }

        return VoidfaringUtility.BestGravshipCrewStat(engine, Core40kDefOf.BEWH_GravshipRangeOffset, 0f, false);
    }
}
