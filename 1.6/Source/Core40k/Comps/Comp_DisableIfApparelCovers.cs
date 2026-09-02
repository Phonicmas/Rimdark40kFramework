using RimWorld;
using Verse;

namespace Core40k;

public class Comp_DisableIfApparelCovers : CompAbilityEffect
{
    private new CompProperties_DisableIfApparelCovers Props => (CompProperties_DisableIfApparelCovers)props;

    public override bool GizmoDisabled(out string reason)
    {
        if (parent.pawn?.apparel == null)
        {
            return base.GizmoDisabled(out reason);
        }

        foreach (var apparel in parent.pawn.apparel.WornApparel)
        {
            foreach (var bodyPart in Props.disabledIfCovered)
            {
                if (!apparel.def.apparel.bodyPartGroups.Contains(bodyPart))
                {
                    continue;
                }
                
                reason = "BEWH.Framework.Comp.AbilityDisabledByCoveredPart".Translate(parent.def.LabelCap, bodyPart.LabelCap, apparel.Label);
                return true;
            }
        }

        return base.GizmoDisabled(out reason);
    }
}