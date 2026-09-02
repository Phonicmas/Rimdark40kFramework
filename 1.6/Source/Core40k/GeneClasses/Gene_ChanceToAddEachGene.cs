using System.Collections.Generic;
using Verse;

namespace Core40k;

public class Gene_ChanceToAddEachGene : Gene
{
    private List<GeneDef> chosenGenes = [];
    
    private DefModExtension_ChanceToAddEachGene GeneDefMod => def.GetModExtension<DefModExtension_ChanceToAddEachGene>();
    
    public override void PostMake()
    {
        base.PostMake();
        if (GeneDefMod != null)
        {
            SelectGeneToGive();
        }
    }
    
    public override void PostAdd()
    {
        base.PostAdd();
        AddSelectedGene();
    }
    
    public override void PostRemove()
    {
        base.PostRemove();
        RemoveSelectedGene();
    }
            
    private void AddSelectedGene()
    {
        if (GeneDefMod == null)
        {
            return;
        }

        if (chosenGenes.NullOrEmpty())
        {
            return;
        }
        
        foreach (var gene in chosenGenes)
        {
            pawn.genes.AddGene(gene, true);
        }
    }
        
    private void RemoveSelectedGene()
    {
        if (GeneDefMod == null)
        {
            return;
        }

        if (chosenGenes.NullOrEmpty())
        {
            return;
        }
        
        foreach (var gene in chosenGenes)
        {
            var gene2 = pawn.genes.GetGene(gene);
            if (gene2 != null)
            {
                pawn.genes.RemoveGene(gene2);
            }
        }
    }

    private void SelectGeneToGive()
    {
        foreach (var gene in GeneDefMod.possibleGenesToGive)
        {
            if (Rand.RangeInclusive(1, 100) > gene.Value)
            {
                continue;
            }
            
            chosenGenes.Add(gene.Key);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref chosenGenes, "chosenGenes");
    }
}