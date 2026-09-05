using Verse;

namespace Core40k;

public class Gene_DisabledBy : Gene
{
    private DefModExtension_GeneDisabledBy cachedExtension;
    private bool extensionResolved;

    private bool evaluating;

    //Active is read on every stat evaluation and every HasActiveGene walk, so the answer is held
    //for the rest of the tick it was computed in.
    private int cachedTick = -1;
    private bool cachedActive;

    public override bool Active
    {
        get
        {
            if (pawn?.genes == null)
            {
                return base.Active;
            }

            var tick = Current.Game?.tickManager?.TicksGame ?? -1;
            if (tick >= 0 && tick == cachedTick)
            {
                return cachedActive;
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
            var active = true;
            try
            {
                foreach (var geneDef in disabledByGenes)
                {
                    if (!pawn.genes.HasActiveGene(geneDef))
                    {
                        continue;
                    }

                    overriddenByGene = pawn.genes.GetGene(geneDef);
                    active = false;
                    break;
                }

                if (active)
                {
                    overriddenByGene = null;
                    active = base.Active;
                }
            }
            finally
            {
                evaluating = false;
            }

            cachedTick = tick;
            cachedActive = active;
            return active;
        }
    }
}
