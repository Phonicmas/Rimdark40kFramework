using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Core40k;

public class WeaponDecorationDef : DecorationDef
{
    public float layerPlacement = 1f;

    public Dictionary<string, DrawData> weaponSpecificDrawData = [];

    public VerbModifier verbModifier = null;

    public List<Tool> tools = null;
    
    public List<VerbProperties> verbs = null;

    public List<string> disablesWeaponTools = null;

    public List<string> disablesWeaponVerbs = null;

    public bool disablesAllWeaponTools = false;

    [Unsaved]
    private bool toolIdsResolved = false;

    public bool AddsToolsOrVerbs => !tools.NullOrEmpty() || !verbs.NullOrEmpty();

    public bool ChangesToolsOrVerbs => AddsToolsOrVerbs || disablesAllWeaponTools || !disablesWeaponTools.NullOrEmpty() || !disablesWeaponVerbs.NullOrEmpty();

    protected override bool AutoDetectUpgrade => base.AutoDetectUpgrade || ChangesToolsOrVerbs || verbModifier != null;

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        if (!verbs.NullOrEmpty())
        {
            foreach (var verbProperties in verbs)
            {
                verbProperties.isPrimary = false;
            }
        }

        if (tools.NullOrEmpty() || toolIdsResolved)
        {
            return;
        }

        toolIdsResolved = true;
        for (var i = 0; i < tools.Count; i++)
        {
            tools[i].id = defName + "_" + (tools[i].id.NullOrEmpty() ? i.ToString() : tools[i].id);
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var configError in base.ConfigErrors())
        {
            yield return configError;
        }

        if (tools.NullOrEmpty())
        {
            yield break;
        }

        foreach (var tool in tools)
        {
            if (tool.label.NullOrEmpty())
            {
                yield return "decoration tool " + tool.id + " has no label";
            }
            if (tool.capacities.NullOrEmpty())
            {
                yield return "decoration tool " + tool.id + " has no capacities, it will never produce an attack";
            }
            if (tool.cooldownTime <= 0f)
            {
                yield return "decoration tool " + tool.id + " has no cooldownTime";
            }
        }

        var duplicate = tools.SelectMany(lhs => tools.Where(rhs => lhs != rhs && lhs.id == rhs.id)).FirstOrDefault();
        if (duplicate != null)
        {
            yield return "duplicate decoration tool id " + duplicate.id;
        }
    }

    public override string TooltipDescription()
    {
        var stringBuilder = new StringBuilder(base.TooltipDescription());

        if (tools.NullOrEmpty())
        {
            return stringBuilder.ToString();
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendLine("BEWH.Framework.Customization.AddedMeleeAttacks".Translate());
        foreach (var tool in tools)
        {
            stringBuilder.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(WeaponDecorationVerbUtility.ToolSummary(tool)));
        }

        return stringBuilder.ToString();
    }
}
