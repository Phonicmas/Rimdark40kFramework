using RimWorld;
using Verse;

namespace Core40k;

public class Comp_DisableInactiveGene : CompAbilityEffect
{
    private new CompProperties_DisableInactiveGene Props => (CompProperties_DisableInactiveGene)props;

    public override bool ShouldHideGizmo
    {
        get
        {
            if (Props.geneDef == null)
            {
                return false;
            }

            return parent.pawn?.genes == null || !parent.pawn.genes.HasActiveGene(Props.geneDef);
        }
    }

    public override bool GizmoDisabled(out string reason)
    {
        if (Props.geneDef != null && (parent.pawn?.genes == null || !parent.pawn.genes.HasActiveGene(Props.geneDef)))
        {
            reason = "BEWH.Framework.Comp.PawnDoesNotHaveRequiredGene".Translate(parent.pawn.LabelShort, Props.geneDef.label.CapitalizeFirst());
            return true;
        }
            
        return base.GizmoDisabled(out reason);
    }
}