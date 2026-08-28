using System.Linq;
using Verse;

namespace Core40k;

public class Gene_DisabledBy : Gene
{
    public override bool Active
    {
        get
        {
            if (pawn?.genes == null)
            {
                return base.Active;
            }
            
            if (def.HasModExtension<DefModExtension_GeneDisabledBy>())
            {
                var disabledByGenes = def.GetModExtension<DefModExtension_GeneDisabledBy>().geneDisabledBy ?? [];
                var overriddenGene = Enumerable.FirstOrDefault(disabledByGenes, gene => pawn.genes.HasActiveGene(gene));
                if (overriddenGene != null)
                {
                    overriddenByGene = pawn.genes.GetGene(overriddenGene);
                    return false;
                }
            }
            return base.Active;
        }
    }
}