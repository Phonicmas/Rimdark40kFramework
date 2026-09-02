using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Core40k;

public static class WeaponDecorationVerbUtility
{
    private static readonly FieldInfo[] ToolFields = typeof(Tool).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    public static Tool CopyTool(Tool source)
    {
        var copy = new Tool();

        foreach (var field in ToolFields)
        {
            field.SetValue(copy, field.GetValue(source));
        }

        copy.capacities = source.capacities != null ? [..source.capacities] : [];
        if (source.extraMeleeDamages != null)
        {
            copy.extraMeleeDamages = [..source.extraMeleeDamages];
        }

        return copy;
    }

    public static bool MatchesAny(this Tool tool, List<string> entries)
    {
        if (tool == null || entries.NullOrEmpty())
        {
            return false;
        }

        foreach (var entry in entries)
        {
            if (entry.NullOrEmpty())
            {
                continue;
            }
            if (entry.EqualsIgnoreCase(tool.id) || entry.EqualsIgnoreCase(tool.label) || entry.EqualsIgnoreCase(tool.untranslatedLabel))
            {
                return true;
            }
        }

        return false;
    }

    public static bool MatchesAny(this VerbProperties verbProperties, List<string> entries)
    {
        if (verbProperties == null || entries.NullOrEmpty())
        {
            return false;
        }

        foreach (var entry in entries)
        {
            if (!entry.NullOrEmpty() && entry.EqualsIgnoreCase(verbProperties.label))
            {
                return true;
            }
        }

        return false;
    }

    public static string ToolSummary(Tool tool)
    {
        var summary = tool.LabelCap + ": " + tool.power.ToString("0.#") + " " + "BEWH.Framework.CommonKeyword.Damage".Translate();

        if (tool.armorPenetration >= 0f)
        {
            summary += ", " + tool.armorPenetration.ToStringPercent() + " " + "BEWH.Framework.CommonKeyword.ArmorPenetration".Translate();
        }

        return summary + ", " + tool.cooldownTime.ToString("0.##") + " " + "SecondsPerAttackLower".Translate();
    }
}
