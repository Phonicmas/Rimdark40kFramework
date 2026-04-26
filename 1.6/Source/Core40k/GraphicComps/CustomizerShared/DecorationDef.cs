using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core40k;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class DecorationDef : Def
{
    [NoTranslate]
    public string iconPath;
            
    [Unsaved]
    private Texture2D icon;
    
    public Texture2D Icon
    {
        get
        {
            if (icon != null)
            {
                return icon;
            }
                    
            icon = !iconPath.NullOrEmpty() ? ContentFinder<Texture2D>.Get(iconPath) : ContentFinder<Texture2D>.Get("NoTex");
            return icon;
        }
    }
    
    [NoTranslate]
    public string drawnTextureIconPath;
        
    public float sortOrder = 0f;
    
    public List<string> appliesTo = new List<string>();
        
    public bool appliesToAll = false;
    
    public DrawData drawData = new();
    public ShaderTypeDef shaderType;
    
    public bool colourable = false;
    public int colorAmount = 1;
        
    public Color? defaultColour;
    public Color? defaultColourTwo;
    public Color? defaultColourThree;
    
    public bool useMask = false;
    public MaskDef defaultMask;
    
    public Vector2 drawSize = Vector2.one;
        
    public List<DecorationColourPresetDef> availablePresets = new();
        
    public List<RankDef> mustHaveRank = null;
        
    public List<GeneDef> mustHaveGene = null;
    
    public List<TraitData> mustHaveTrait = null;
    
    public List<HediffDef> mustHaveHediff = null;
    
    public List<DecorationFlag> decorationFlags = new();
    
    public List<StatModifier> statOffsets = new List<StatModifier>();

    public List<StatModifier> statFactors = new List<StatModifier>();
    
    public List<ConditionalStatAffecter> conditionalStatAffecters = new List<ConditionalStatAffecter>();

    public string TooltipDescription()
    {
        var stringbuilder = new StringBuilder();
        stringbuilder.AppendLine(label);

        if (!statOffsets.NullOrEmpty())
        {
            stringbuilder.AppendLine();
            stringbuilder.AppendLine("BEWH.Framework.CommonKeyword.StatOffset".Translate());
            foreach (var statOffset in statOffsets)
            {
                stringbuilder.AppendLine(statOffset.stat.label.CapitalizeFirst() + ": " + statOffset.ValueToStringAsOffset);
            }
        }
        
        if (!statFactors.NullOrEmpty())
        {
            stringbuilder.AppendLine();
            stringbuilder.AppendLine("BEWH.Framework.CommonKeyword.StatFactor".Translate());
            foreach (var statFactor in statFactors)
            {
                stringbuilder.AppendLine(statFactor.stat.label.CapitalizeFirst() + ": x" + statFactor.ValueToStringAsOffset);
            }
        }

        if (!conditionalStatAffecters.NullOrEmpty())
        {
            foreach (var conditionalStatAffecter in conditionalStatAffecters)
            {
                if (!conditionalStatAffecter.statOffsets.NullOrEmpty())
                {
                    stringbuilder.AppendLine();
                    stringbuilder.AppendLine("BEWH.Framework.CommonKeyword.StatOffset".Translate());
                    foreach (var statOffset in conditionalStatAffecter.statOffsets)
                    {
                        stringbuilder.AppendLine(statOffset.stat.label.CapitalizeFirst() + ": " + statOffset.ValueToStringAsOffset);
                    }
                }
        
                if (!conditionalStatAffecter.statFactors.NullOrEmpty())
                {
                    stringbuilder.AppendLine();
                    stringbuilder.AppendLine("BEWH.Framework.CommonKeyword.StatFactor".Translate());
                    foreach (var statFactor in conditionalStatAffecter.statFactors)
                    {
                        stringbuilder.AppendLine(statFactor.stat.label.CapitalizeFirst() + ": x" + statFactor.ValueToStringAsOffset);
                    }
                }
            }
        }
        
        return stringbuilder.ToString();
    }
    
    public virtual bool HasRequirements(Pawn pawn, out string lockedReason)
    {
        var reason = new StringBuilder();
        var requirementFulfilled = true;
        if (mustHaveRank != null)
        {
            if (!pawn.HasComp<CompRankInfo>())
            {
                reason.AppendLine("COMP ISSUE: SHOW PHONICMAS");
                requirementFulfilled = false;
            }
            var comp = pawn.GetComp<CompRankInfo>();
            var missingRanks = (from rank in mustHaveRank where !comp.HasRank(rank) select rank.label.CapitalizeFirst()).ToList();
            if (missingRanks.Count > 0)
            {
                requirementFulfilled = false;
                reason.AppendLine("BEWH.Framework.Customization.MissingRanks".Translate());
                foreach (var rank in missingRanks)
                {
                    reason.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(rank));
                }
            }
        }
    
        if (mustHaveGene != null)
        {
            if (pawn.genes == null)
            {
                requirementFulfilled = false;
            }
            
            var missingGenes = (from gene in mustHaveGene where !pawn.genes.HasActiveGene(gene) select gene.label.CapitalizeFirst()).ToList();
            if (missingGenes.Count > 0)
            {
                requirementFulfilled = false;
                reason.AppendLine("BEWH.Framework.Customization.MissingGenes".Translate());
                foreach (var gene in missingGenes)
                {
                    reason.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(gene));
                }
            }
        }

        if (mustHaveTrait != null)
        {
            if (pawn.story?.traits == null)
            {
                requirementFulfilled = false;
            }
            
            var missingTraits = (from trait in mustHaveTrait where !pawn.story.traits.HasTrait(trait.traitDef, trait.degree) select trait.traitDef.label.CapitalizeFirst()).ToList();
            if (missingTraits.Count > 0)
            {
                requirementFulfilled = false;
                reason.AppendLine("BEWH.Framework.Customization.MissingTraits".Translate());
                foreach (var trait in missingTraits)
                {
                    reason.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(trait));
                }
            }
        }

        if (mustHaveHediff != null)
        {
            if (pawn.health?.hediffSet == null)
            {
                requirementFulfilled = false;
            }
            
            
            var missingHediffs = (from hediff in mustHaveHediff where !pawn.health.hediffSet.HasHediff(hediff) select hediff.label.CapitalizeFirst()).ToList();
            if (missingHediffs.Count > 0)
            {
                requirementFulfilled = false;
                reason.AppendLine("BEWH.Framework.Customization.MissingHediffs".Translate());
                foreach (var hediff in missingHediffs)
                {
                    reason.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(hediff));
                }
            }
        }
            
        lockedReason = reason.ToString();
        return requirementFulfilled;
    }
    
    public override void ResolveReferences()
    {
        base.ResolveReferences();
        shaderType ??= ShaderTypeDefOf.Cutout;
    }
}

public class DecorationFlag
{
    public string flag;
    public string newTexPath;
    public string maskPathAddition;
    public ShaderTypeDef shaderType = null;
    public int priority = 0;
}