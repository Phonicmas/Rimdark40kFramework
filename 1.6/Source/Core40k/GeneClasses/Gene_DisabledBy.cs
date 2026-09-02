using System.Collections.Generic;
using Verse;

namespace Core40k;

public class Gene_DisabledBy : Gene
{
    //Active is read constantly - every stat, every capacity, every walk of the gene list - so the
    //mod extension is resolved once instead of two linear scans of def.modExtensions per call.
    private DefModExtension_GeneDisabledBy cachedExtension;
    private bool extensionResolved;

    private bool evaluating;

    public override bool Active
    {
        get
        {
            if (pawn?.genes == null)
            {
                return base.Active;
            }

            if (!extensionResolved)
            {
                cachedExtension = def.GetModExtension<DefModExtension_GeneDisabledBy>();
                extensionResolved = true;
            }

            var disabledByGenes = cachedExtension?.geneDisabledBy;
            if (disabledByGenes.NullOrEmpty())
            {
                return base.Active;
            }

            //HasActiveGene evaluates other genes' Active, so two of these genes listing each other
            //recursed until the stack ran out, with no error to show for it. A re-entrant call
            //falls through to the base answer instead.
            if (evaluating)
            {
                return base.Active;
            }

            evaluating = true;
            try
            {
                foreach (var geneDef in disabledByGenes)
                {
                    if (!pawn.genes.HasActiveGene(geneDef))
                    {
                        continue;
                    }

                    overriddenByGene = pawn.genes.GetGene(geneDef);
                    return false;
                }

                //Cleared when nothing disables it any more. This was only ever set, so once a
                //disabling gene was removed the gene stayed reported as overridden and never came
                //back on.
                overriddenByGene = null;
            }
            finally
            {
                evaluating = false;
            }

            return base.Active;
        }
    }
}
