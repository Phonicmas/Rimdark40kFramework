using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class StatPart_DecorationSlots : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        val += OffsetFor(req);
    }

    public override string ExplanationPart(StatRequest req)
    {
        var offset = OffsetFor(req);
        if (Mathf.Approximately(offset, 0f))
        {
            return null;
        }

        var builder = new StringBuilder();
        builder.AppendLine("BEWH.Framework.StatReport.Decoration".Translate() + ": " +
                           parentStat.Worker.ValueToString(offset, false, ToStringNumberSense.Offset));
        return builder.ToString();
    }

    private float OffsetFor(StatRequest req)
    {
        if (!req.HasThing || req.Thing is not ThingWithComps thing || thing.AllComps == null)
        {
            return 0f;
        }

        var offset = 0f;
        foreach (var comp in thing.AllComps)
        {
            if (comp is CompDecorativeBase decorative)
            {
                offset += decorative.GetStatOffset(parentStat);
            }
        }

        return offset;
    }
}
