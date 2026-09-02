using Verse;

namespace Core40k;

public class Comp_GivesAbility : ThingComp
{
    private CompProperties_GivesAbility Props => (CompProperties_GivesAbility)props;

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);

        if (Props.ability == null || pawn?.abilities == null)
        {
            return;
        }

        pawn.abilities.GainAbility(Props.ability);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);

        if (Props.ability == null || pawn?.abilities == null)
        {
            return;
        }

        if (StillGrantedByAnythingElse(pawn))
        {
            return;
        }

        pawn.abilities.RemoveAbility(Props.ability);
    }

    private bool StillGrantedByAnythingElse(Pawn pawn)
    {
        if (pawn.apparel?.WornApparel != null)
        {
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel != parent && GrantsSameAbility(apparel))
                {
                    return true;
                }
            }
        }

        if (pawn.equipment?.AllEquipmentListForReading != null)
        {
            foreach (var equipment in pawn.equipment.AllEquipmentListForReading)
            {
                if (equipment != parent && GrantsSameAbility(equipment))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool GrantsSameAbility(ThingWithComps thing)
    {
        foreach (var comp in thing.AllComps)
        {
            if (comp is Comp_GivesAbility givesAbility && givesAbility.Props.ability == Props.ability)
            {
                return true;
            }
        }

        return false;
    }
}
