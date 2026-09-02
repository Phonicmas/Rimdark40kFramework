using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace Core40k;

public class DefModExtension_AmmoChanger : DefModExtension
{
    public ResearchProjectDef unlockedBy = null;
    public float? effectiveRange;
    public float? warmupTime;
    public int? shotsPerBurst;
    
    public List<StatModifier> statOffsets = [];
    public List<StatModifier> statFactors = [];

    public string StatSummary()
    {
        var stringBuilder = new StringBuilder();

        if (shotsPerBurst.HasValue)
        {
            stringBuilder.AppendLine("BEWH.Framework.AmmoChanger.BurstCount".Translate(shotsPerBurst.Value));
        }

        if (effectiveRange.HasValue)
        {
            stringBuilder.AppendLine("BEWH.Framework.AmmoChanger.Range".Translate(effectiveRange.Value.ToString("0.#")));
        }

        if (warmupTime.HasValue)
        {
            stringBuilder.AppendLine("BEWH.Framework.AmmoChanger.Warmup".Translate(warmupTime.Value.ToString("0.##")));
        }

        if (!statOffsets.NullOrEmpty())
        {
            foreach (var statOffset in statOffsets)
            {
                stringBuilder.AppendLine(statOffset.stat.LabelCap + ": " + Core40kUtils.ValueToString(statOffset.stat, statOffset.value, finalized: false, ToStringNumberSense.Offset));
            }
        }

        if (!statFactors.NullOrEmpty())
        {
            foreach (var statFactor in statFactors)
            {
                stringBuilder.AppendLine(statFactor.stat.LabelCap + ": " + Core40kUtils.ValueToString(statFactor.stat, statFactor.value, finalized: false, ToStringNumberSense.Factor));
            }
        }

        return stringBuilder.ToString().TrimEndNewlines();
    }
}
