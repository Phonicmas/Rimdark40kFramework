using RimWorld;
using Verse;

namespace Core40k;

public class CompAbilityEffect_MustHaveGene : CompAbilityEffect
{
    private new CompProperties_MustHaveGene Props => (CompProperties_MustHaveGene)props;
        
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        base.Valid(target, throwMessages);

        if (target.Pawn is not { } pawn)
        {
            return false;
        }

        return pawn.genes != null && pawn.genes.HasActiveGene(Props.geneDef);
    }
        
    public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
    {
        base.ExtraLabelMouseAttachment(target);

        if (target.Pawn is not { } pawn)
        {
            return null;
        }

        if (pawn.genes == null || !pawn.genes.HasActiveGene(Props.geneDef))
        {
            return "BEWH.Framework.Comp.PawnDoesNotHaveRequiredGene".Translate(pawn, Props.geneDef.label);
        }
            
        return null;
    }
}
