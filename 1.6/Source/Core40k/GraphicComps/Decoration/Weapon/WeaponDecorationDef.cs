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

    //Extra melee attacks this decoration grants to the weapon it sits on, e.g. a bayonet.
    //Written exactly like ThingDef.tools.
    public List<Tool> tools = null;

    //Extra verbs this decoration grants, e.g. an underbarrel grenade launcher.
    //Written exactly like ThingDef.verbs. These get their own gizmo, they are never the
    //weapons primary verb and pawns will not pick them on their own.
    public List<VerbProperties> verbs = null;

    //Tools on the host weapon that get suppressed while this decoration is attached.
    //Matches against Tool.id or Tool.label, so a bayonet can remove the stock bash.
    public List<string> disablesWeaponTools = null;

    //Verbs on the host weapon that get suppressed while this decoration is attached.
    //Matches against VerbProperties.label.
    public List<string> disablesWeaponVerbs = null;

    public bool disablesAllWeaponTools = false;

    [Unsaved]
    private bool toolIdsResolved = false;

    public bool AddsToolsOrVerbs => !tools.NullOrEmpty() || !verbs.NullOrEmpty();

    public bool ChangesToolsOrVerbs => AddsToolsOrVerbs || disablesAllWeaponTools || !disablesWeaponTools.NullOrEmpty() || !disablesWeaponVerbs.NullOrEmpty();

    //A bayonet is an upgrade even with no stat offsets on it.
    protected override bool AutoDetectUpgrade => base.AutoDetectUpgrade || ChangesToolsOrVerbs || verbModifier != null;

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        if (!verbs.NullOrEmpty())
        {
            foreach (var verbProperties in verbs)
            {
                //The host weapon keeps its own primary verb, a decoration must never take it over.
                verbProperties.isPrimary = false;
            }
        }

        if (tools.NullOrEmpty() || toolIdsResolved)
        {
            return;
        }

        toolIdsResolved = true;
        //ThingDef hands its own tools plain index ids ("0", "1", ...), so decoration tools are
        //namespaced with the defName to keep verb load ids unique on whatever weapon they attach to.
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
