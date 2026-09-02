using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace Core40k;

public class CompGraphicParent : ThingComp
{
    //RankDef's pattern: these types outlive a game, so the cache has to be keyed on the game it
    //came from. A plain ??= keeps handing out the previous save's component after loading a second
    //save in the same session, which pins every pawn and item from the old game in memory and
    //answers every lookup from the wrong colony.
    private static Game cachedGameForCoreUtils;
    private static GameComponent_CoreUtils coreUtils;

    protected static GameComponent_CoreUtils CoreUtils => CoreUtilsFor();

    private static GameComponent_CoreUtils CoreUtilsFor()
    {
        if (coreUtils != null && cachedGameForCoreUtils == Current.Game)
        {
            return coreUtils;
        }

        cachedGameForCoreUtils = Current.Game;
        coreUtils = cachedGameForCoreUtils?.GetComponent<GameComponent_CoreUtils>();

        return coreUtils;
    }
    
    protected Dictionary<StatDef, float> cachedStatOffset = new();
    public Dictionary<StatDef, float> CachedStatOffset => cachedStatOffset ??= new Dictionary<StatDef, float>();
    protected Dictionary<StatDef, float> cachedStatFactor = new();
    public Dictionary<StatDef, float> CachedStatFactor => cachedStatFactor ??= new Dictionary<StatDef, float>();
    
    protected ThingDef ThingDef => parent.def;
    protected Thing Thing => parent;
    
    protected bool IsApparel => parent is Apparel;

    protected Pawn Wearer => ParentHolder is not Pawn_ApparelTracker pawn_ApparelTracker ? null : pawn_ApparelTracker.pawn;
    protected Pawn Holder => ParentHolder is not Pawn_EquipmentTracker pawn_EquipmentTracker ? null : pawn_EquipmentTracker.pawn;

    protected Pawn Pawn => Wearer ?? Holder;

    private bool initialSet;

    public bool InitialSet
    {
        get => initialSet;
        set => initialSet = value;
    }
    
    public virtual void Notify_GraphicChanged()
    {
        cachedStatOffset = new Dictionary<StatDef, float>();
        cachedStatFactor = new Dictionary<StatDef, float>();
        parent.Notify_ColorChanged();
    }
    
    //One contribution to a stat from something fitted to the item, carrying the heading it should
    //be reported under so internal upgrades read separately from visible decorations.
    public readonly struct StatContribution
    {
        public readonly string label;
        public readonly StatModifier modifier;
        public readonly string groupKey;
        public readonly int groupOrder;

        public StatContribution(string label, StatModifier modifier, string groupKey, int groupOrder = 0)
        {
            this.label = label;
            this.modifier = modifier;
            this.groupKey = groupKey;
            this.groupOrder = groupOrder;
        }
    }

    //Info card entry for a stat this comp contributes to the item.
    //
    //Built with the string label StatDrawEntry constructor rather than the
    //(category, stat, valueString) one, deliberately. That constructor keeps the StatDef attached
    //but leaves the entry's own value at 0, and StatDrawEntry.GetExplanationText then appends
    //stat.Worker.GetExplanationFull(...) for whatever the info card is showing. On a piece of
    //apparel that runs the WEARER's calculation of the stat against the armour itself, so Move
    //Speed reported "Base value 3" and "Final value 0" on a helmet: a base the helmet does not
    //have, and a final that is not what anyone ends up with. With no StatDef attached the entry
    //shows exactly the report built here and nothing else.
    protected static StatDrawEntry StatContributionEntry(StatCategoryDef category, StatDef stat, List<StatContribution> contributions, bool isFactor)
    {
        var numberSense = isFactor ? ToStringNumberSense.Factor : ToStringNumberSense.Offset;
        var total = isFactor ? 1f : 0f;

        //Grouped under their own headings, in a fixed order rather than whatever order the comp
        //happened to walk its collection in.
        var groupKeys = new List<string>();
        var groupOrders = new Dictionary<string, int>();
        var groups = new Dictionary<string, List<StatContribution>>();

        foreach (var contribution in contributions)
        {
            //Offsets add, factors multiply, matching GetStatOffset and GetStatFactor on the comps,
            //which is what the wearer actually gets. The factor row used to sum its modifiers, so
            //two x1.1 upgrades advertised x2.2 while applying x1.21.
            if (isFactor)
            {
                total *= contribution.modifier.value;
            }
            else
            {
                total += contribution.modifier.value;
            }

            if (!groups.TryGetValue(contribution.groupKey, out var group))
            {
                group = [];
                groups.Add(contribution.groupKey, group);
                groupOrders.Add(contribution.groupKey, contribution.groupOrder);
                groupKeys.Add(contribution.groupKey);
            }

            group.Add(contribution);
        }

        groupKeys.Sort((first, second) => groupOrders[first].CompareTo(groupOrders[second]));

        var report = new StringBuilder();
        if (!stat.description.NullOrEmpty())
        {
            report.AppendLine(stat.description);
            report.AppendLine();
        }

        foreach (var groupKey in groupKeys)
        {
            report.AppendLine(groupKey.Translate() + ":");
            foreach (var contribution in groups[groupKey])
            {
                report.AppendLine("    " + contribution.label + ": " + stat.Worker.ValueToString(contribution.modifier.value, false, numberSense));
            }
        }

        var totalString = stat.Worker.ValueToString(total, false, numberSense);

        //Only worth a total line when more than one thing is contributing; with a single
        //contributor it would just repeat the line above it.
        if (contributions.Count > 1)
        {
            report.AppendLine();
            report.AppendLine("BEWH.Framework.StatReport.Total".Translate() + ": " + totalString);
        }

        return new StatDrawEntry(
            category,
            stat.LabelCap,
            totalString,
            report.ToString().TrimEndNewlines(),
            stat.displayPriorityInCategory);
    }

    public virtual void SetOriginals()
    {
    }

    public virtual void Reset()
    {
    }

    //Deferred changes.
    //
    //Accepting the customization dialog no longer applies anything straight away. The edited state
    //is snapshotted, the comp is rolled back to what it was, and the pawn works at the station for
    //the accumulated work amount. Only when that work finishes is the snapshot committed and the
    //resource cost taken.
    //
    //Two sets of members, because the numbers are needed at two different moments:
    //  Edit*    - live state versus committed state. Used to price the change in the confirm
    //             dialog, before anything has been captured.
    //  Pending* - the captured snapshot. Used by the job once the live state has been rolled back.

    //Live state differs from the committed state.
    public virtual bool HasEdits => false;

    //Live colour / mask / alternate form differs. Drives the flat per item appearance charge.
    public virtual bool HasAppearanceEdit => false;

    //Structural work (things added and removed) for the live edits. Excludes the appearance charge.
    public virtual float EditWork => 0f;

    public virtual void CollectEditCost(List<ThingDefCountClass> into)
    {
    }

    public virtual bool HasPendingChange => false;

    public virtual bool PendingAppearanceChange => false;

    public virtual float PendingWork => 0f;

    public virtual void CollectPendingCost(List<ThingDefCountClass> into)
    {
    }

    //Snapshot the live state and roll back to the committed state.
    public virtual void CapturePending()
    {
    }

    //Apply the snapshot for real.
    public virtual void CommitPending()
    {
    }

    //Throw the snapshot away, leaving the committed state untouched.
    public virtual void DiscardPending()
    {
    }
    
    public virtual void InitialSetup()
    {
        SetOriginals();
        initialSet = true;
    }
    
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (initialSet)
        {
            return;
        }
        InitialSetup();
    }
    
    public override void PostExposeData()
    {
        Scribe_Values.Look(ref initialSet, "initialColourSet");
        base.PostExposeData();
    }
}