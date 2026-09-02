using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

public class Gene_AddRandomGeneAndOrTraitByWeight : Gene
{
    private GeneDef chosenGene = null;
    private List<GeneDef> chosenGenes = [];

    private TraitDef chosenTrait = null;
    private int chosenTraitDegree = 0;
    private Dictionary<TraitDef, int> chosenTraits = new Dictionary<TraitDef, int>();
    
    private DefModExtension_AddRandomGeneByWeight GeneDefMod => def.GetModExtension<DefModExtension_AddRandomGeneByWeight>();
    private DefModExtension_AddRandomTraitByWeight TraitDefMod => def.GetModExtension<DefModExtension_AddRandomTraitByWeight>();
    
    public override void PostMake()
    {
        base.PostMake();
        if (GeneDefMod != null)
        {
            SelectGeneToGive();
        }

        if (TraitDefMod != null)
        {
            SelectTraitToGive();
        }
    }
    
    public override void PostAdd()
    {
        base.PostAdd();
        AddSelectedTraitAndGene();
    }
    public override void PostRemove()
    {
        base.PostRemove();
        RemoveSelectedTraitAndGene();
    }
            
    private void AddSelectedTraitAndGene()
    {
        if (GeneDefMod != null)
        {
            if (chosenGene != null)
            {
                pawn.genes.AddGene(chosenGene, GeneDefMod.addAsXenogene);
            }
            
            if (!chosenGenes.NullOrEmpty())
            {
                foreach (var gene in chosenGenes)
                {
                    pawn.genes.AddGene(gene, GeneDefMod.addAsXenogene);
                }
            }
        }

        if (TraitDefMod != null)
        {
            if (chosenTrait != null)
            {
                var trait = new Trait(chosenTrait, chosenTraitDegree);
                pawn.story.traits.GainTrait(trait);
            }
            
            if (!chosenTraits.NullOrEmpty())
            {
                foreach (var traitPair in chosenTraits)
                {
                    var trait = new Trait(traitPair.Key, traitPair.Value);
                    pawn.story.traits.GainTrait(trait);
                }
            }
        }
    }
        
    private void RemoveSelectedTraitAndGene()
    {
        if (GeneDefMod != null)
        {
            if (chosenGene != null)
            {
                var gene = pawn.genes.GetGene(chosenGene);
                if (gene != null)
                {
                    pawn.genes.RemoveGene(gene);
                }
            }
            
            if (!chosenGenes.NullOrEmpty())
            {
                foreach (var gene in chosenGenes)
                {
                    var gene2 = pawn.genes.GetGene(gene);
                    if (gene2 != null)
                    {
                        pawn.genes.RemoveGene(gene2);
                    }
                }   
            }
        }
        
        if (TraitDefMod != null)
        {
            if (chosenTrait != null)
            {
                var trait = pawn.story.traits.GetTrait(chosenTrait);
                if (trait != null)
                {
                    pawn.story.traits.RemoveTrait(trait);
                }
            }
            
            if (!chosenTraits.NullOrEmpty())
            {
                foreach (var traitPair in chosenTraits)
                {
                    var trait = pawn.story.traits.GetTrait(traitPair.Key);
                    if (trait != null)
                    {
                        pawn.story.traits.RemoveTrait(trait);
                    }
                }
            }
        }
    }

    private void SelectTraitToGive()
    {
        if (Rand.RangeInclusive(1, 100) > TraitDefMod.chanceToGrantTrait)
        {
            return;
        }
        
        var possibleTraits = TraitDefMod.possibleTraitsToGive.Where(g => !pawn.story.traits.HasTrait(g.traitDef, g.degree)).ToList();
        if (possibleTraits.NullOrEmpty())
        {
            return;
        }
        
        var weightedSelection = new WeightedSelection<TraitData>();
        foreach (var trait in possibleTraits)
        {
            weightedSelection.AddEntry(new TraitData(trait.traitDef, trait.degree), trait.weight);
        }

        if (TraitDefMod.amountToGive == 1)
        {
            var result = weightedSelection.GetRandom();
        
            chosenTrait = result.traitDef;
            chosenTraitDegree = result.degree;
        }
        else if (TraitDefMod.amountToGive == possibleTraits.Count)
        {
            foreach (var traitData in possibleTraits)
            {
                if (chosenTraits.ContainsKey(traitData.traitDef))
                {
                    continue;
                }
                chosenTraits.Add(traitData.traitDef, traitData.degree);
            }
        }
        else
        {
            var traitsToGive = Math.Min(TraitDefMod.amountToGive, possibleTraits.Count);
            for (var i = 0; i < traitsToGive; i++)
            {
                var result = weightedSelection.GetRandomUnique();
                if (result?.traitDef == null || chosenTraits.ContainsKey(result.traitDef))
                {
                    continue;
                }

                chosenTraits.Add(result.traitDef, result.degree);
            }
        }
    }

    private void SelectGeneToGive()
    {
        if (Rand.RangeInclusive(1, 100) > GeneDefMod.chanceToGrantGene)
        {
            return;
        }
        
        var possibleGenes = GeneDefMod.possibleGenesToGive.Where(g => !pawn.genes.HasActiveGene(g.Key)).ToList();
        if (possibleGenes.NullOrEmpty())
        {
            return;
        }

        if (GeneDefMod.skipIfAnyAlreadyExistsOnPawn && possibleGenes.Count < GeneDefMod.possibleGenesToGive.Count)
        {
            return;
        }
        
        var weightedSelection = new WeightedSelection<GeneDef>();
            
        foreach (var gene in possibleGenes)
        {
            weightedSelection.AddEntry(gene.Key, gene.Value);
        }

        var amountToGive = GeneDefMod.amountToGive.RandomInRange;
        
        if (amountToGive == 1)
        {
            chosenGene = weightedSelection.GetRandom();
        }
        else if (amountToGive == possibleGenes.Count)
        {
            chosenGenes.AddRangeUnique(possibleGenes.Select(pair => pair.Key));
        }
        else
        {
            var genesToGive = Math.Min(amountToGive, possibleGenes.Count);
            for (var i = 0; i < genesToGive; i++)
            {
                var result = weightedSelection.GetRandomUnique();
                if (result == null || chosenGenes.Contains(result))
                {
                    continue;
                }

                chosenGenes.Add(result);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref chosenGene, "chosenGene");
        Scribe_Defs.Look(ref chosenTrait, "chosenTrait");
        Scribe_Collections.Look(ref chosenGenes, "chosenGenes");
        Scribe_Collections.Look(ref chosenTraits, "chosenTraits");
        Scribe_Values.Look(ref chosenTraitDegree, "chosenTraitDegree", 0);
    }
}