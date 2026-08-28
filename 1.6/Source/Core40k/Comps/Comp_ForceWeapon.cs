using RimWorld;
using System.Linq;
using Verse;

namespace Core40k;

public class Comp_ForceWeapon : ThingComp
{
    public CompProperties_ForceWeapon Props => (CompProperties_ForceWeapon)props;

    private float statValue = -1;

    public const string ExtraDamageName = "Custom extra damage";

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        CalculateExtraDamage(pawn);
    }

    public override void Notify_UsedWeapon(Pawn pawn)
    {
        if (pawn.GetStatValue(Props.scalingStat) != statValue)
        {
            CalculateExtraDamage(pawn);
        }
    }

    private void CalculateExtraDamage(Pawn pawn)
    {
        statValue = pawn.GetStatValue(Props.scalingStat);
        
        if (pawn == null || statValue <= 0 || statValue < Props.minValueToWork)
        {
            return;
        }

        var cachedDamageValue = statValue * Props.damageScalingFactor;
        var cachedPenValue = Props.flatPen;

        if (Props.scalesPen)
        {
            cachedPenValue = pawn.GetStatValue(Props.scalingStat) * Props.penScaleFactor;
        }

        if (parent.TryGetComp<CompEquippable>().Tools == null) return;
            
        foreach (var tool in parent.TryGetComp<CompEquippable>().Tools)
        {
            if (tool.capacities.Intersect(Props.capacitiesToApplyOn).EnumerableNullOrEmpty())
            {
                continue;
            }
                    
            if (tool.extraMeleeDamages.NullOrEmpty())
            {
                tool.extraMeleeDamages = [];
            }
            else
            {
                tool.extraMeleeDamages.RemoveWhere(damage => damage is NamedExtraDamage);
            }
            
            var extraDamage = new NamedExtraDamage
            {
                def = Props.damageDef,
                amount = cachedDamageValue,
                armorPenetration = cachedPenValue,
                name = ExtraDamageName
            };
            
            tool.extraMeleeDamages.Add(extraDamage);
        }
    }
}