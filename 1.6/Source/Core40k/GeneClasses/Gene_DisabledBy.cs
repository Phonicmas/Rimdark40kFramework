using Verse;

namespace Core40k;

public class Gene_DisabledBy : Gene
{
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
