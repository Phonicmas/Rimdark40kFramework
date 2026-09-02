using RimWorld;
using Verse;

namespace Core40k;

public class Recipe_InstallImplantRequiringHediff : Recipe_InstallImplant
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        var defMod = recipe?.GetModExtension<DefModExtension_RequiresHediff>();
        if (defMod?.hediffDef == null)
        {
            return false;
        }

        if (thing is not Pawn pawn || pawn.health?.hediffSet == null || !pawn.health.hediffSet.HasHediff(defMod.hediffDef))
        {
            return false;
        }

        return base.AvailableOnNow(thing, part);
    }
}