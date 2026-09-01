using Verse;

namespace Core40k;

public enum TabTargetKind
{
    Any = 0,
    Apparel = 1,
    Equipment = 2,
}

//Decides whether a customization tab shows up for a given ThingDef.
//A tab applies when the item carries the comp(s) the tab's drawer operates on, and there is at
//least one piece of content for it. Content presence is checked per ThingDef and is independent of
//the pawn, so a tab still appears when every entry in it is requirement locked for the current
//pawn - a greyed out entry with its requirements in the tooltip is how the player learns it exists.
public abstract class CustomizationTabWorker
{
    public CustomizationTabDef def;

    public abstract bool AppliesTo(ThingDef thingDef);

    protected bool MatchesKind(ThingDef thingDef)
    {
        if (thingDef == null)
        {
            return false;
        }

        return def.targetKind switch
        {
            TabTargetKind.Apparel => thingDef.IsApparel,
            TabTargetKind.Equipment => thingDef.IsWeapon,
            _ => true,
        };
    }

    protected bool HasRequiredComps(ThingDef thingDef)
    {
        foreach (var compType in def.requiredComps)
        {
            if (!thingDef.HasCompAssignable(compType))
            {
                return false;
            }
        }

        return true;
    }
}

//Comps only. Used by the coloring tabs, where having CompMultiColor is the whole requirement.
public class CustomizationTabWorker_Comp : CustomizationTabWorker
{
    public override bool AppliesTo(ThingDef thingDef)
    {
        return MatchesKind(thingDef) && HasRequiredComps(thingDef);
    }
}

//Comps plus at least one applicable cosmetic decoration.
public class CustomizationTabWorker_Decoration : CustomizationTabWorker_Comp
{
    public override bool AppliesTo(ThingDef thingDef)
    {
        return base.AppliesTo(thingDef) && DecorationIndex.HasDecorations(thingDef, upgrades: false);
    }
}

//Comps plus at least one applicable upgrade.
public class CustomizationTabWorker_Upgrade : CustomizationTabWorker_Comp
{
    public override bool AppliesTo(ThingDef thingDef)
    {
        return base.AppliesTo(thingDef) && DecorationIndex.HasDecorations(thingDef, upgrades: true);
    }
}

//Comps plus at least one alternate base form.
public class CustomizationTabWorker_AlternateTexture : CustomizationTabWorker_Comp
{
    public override bool AppliesTo(ThingDef thingDef)
    {
        return base.AppliesTo(thingDef) && DecorationIndex.AlternatesFor(thingDef).Count > 0;
    }
}
