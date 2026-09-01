using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

//Lets an attached upgrade grant the item more of a stat that lives on the item itself, rather than
//on the pawn wearing it. Internal slots are the case this exists for: a chassis expansion upgrade
//can raise how many internal slots the armour has.
//
//The pawn-facing path (StatOffsetFromGear) only covers stats read off the wearer, so without this
//the item's own stat would show its base value only. CompDecorativeBase.TotalInternalSlots computes
//the same sum directly, because the UI needs a live answer while the customization dialog is open
//and the stat system caches; this part is what keeps the info card agreeing with it.
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
