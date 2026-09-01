using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class Comp_ForceWeapon : ThingComp
{
    public CompProperties_ForceWeapon Props => (CompProperties_ForceWeapon)props;

    //Last scaling stat value seen for the pawn holding this weapon. -1 means nobody is holding it,
    //which is what makes the bonus go away again on unequip. Scribed so a loaded save keeps the
    //bonus instead of losing it until the weapon is re-equipped.
    private float statValue = -1f;

    public const string ExtraDamageName = "Custom extra damage";

    //Per instance tool copies. Rebuilt when the list handed to us changes (a decoration was added
    //or removed) or when the stat moved.
    private List<Tool> cachedTools;
    private List<Tool> cachedSourceTools;
    private float cachedStatValue = -1f;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref statValue, "statValue", -1f);
    }

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        RefreshStatValue(pawn);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        statValue = -1f;
        InvalidateTools();
    }

    public override void Notify_UsedWeapon(Pawn pawn)
    {
        base.Notify_UsedWeapon(pawn);
        RefreshStatValue(pawn);
    }

    private void RefreshStatValue(Pawn pawn)
    {
        if (pawn == null || Props.scalingStat == null)
        {
            return;
        }

        var newValue = pawn.GetStatValue(Props.scalingStat);
        if (Mathf.Approximately(newValue, statValue))
        {
            return;
        }

        statValue = newValue;
        InvalidateTools();
    }

    private void InvalidateTools()
    {
        cachedTools = null;
        cachedSourceTools = null;
        cachedStatValue = -1f;
    }

    //Returns a per instance copy of the given tool list carrying the scaled extra damage.
    //The list handed in is never written to: for an undecorated weapon it is parent.def.tools,
    //which every other weapon of that def shares, so mutating it leaks one pawn's bonus onto all
    //of them (including weapons in storage and weapons held by enemies) for the rest of the session.
    public List<Tool> ApplyExtraDamage(List<Tool> sourceTools)
    {
        if (sourceTools.NullOrEmpty() || Props.damageDef == null || Props.capacitiesToApplyOn.NullOrEmpty())
        {
            return sourceTools;
        }

        if (statValue <= 0f || statValue < Props.minValueToWork)
        {
            return sourceTools;
        }

        if (cachedTools != null && ReferenceEquals(cachedSourceTools, sourceTools) && Mathf.Approximately(cachedStatValue, statValue))
        {
            return cachedTools;
        }

        var cachedDamageValue = statValue * Props.damageScalingFactor;
        var cachedPenValue = Props.scalesPen ? statValue * Props.penScaleFactor : Props.flatPen;

        var tools = new List<Tool>(sourceTools.Count);
        foreach (var tool in sourceTools)
        {
            if (tool == null || tool.capacities.NullOrEmpty() || tool.capacities.Intersect(Props.capacitiesToApplyOn).EnumerableNullOrEmpty())
            {
                tools.Add(tool);
                continue;
            }

            var copy = WeaponDecorationVerbUtility.CopyTool(tool);
            copy.extraMeleeDamages ??= [];
            copy.extraMeleeDamages.RemoveWhere(damage => damage is NamedExtraDamage);
            copy.extraMeleeDamages.Add(new NamedExtraDamage
            {
                def = Props.damageDef,
                amount = cachedDamageValue,
                armorPenetration = cachedPenValue,
                name = ExtraDamageName,
            });
            tools.Add(copy);
        }

        cachedSourceTools = sourceTools;
        cachedStatValue = statValue;
        cachedTools = tools;
        return tools;
    }
}
