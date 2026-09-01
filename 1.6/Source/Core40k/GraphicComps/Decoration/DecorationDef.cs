using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    
    public List<string> appliesTo = [];
    public bool appliesToAll = false;
    
    public DrawData drawData = new();
    public ShaderTypeDef shaderType;
    public Vector2 drawSize = Vector2.one;
    
    public bool colourable = false;
    public int colorAmount = 1;
        
    public Color? defaultColour;
    public Color? defaultColourTwo;
    public Color? defaultColourThree;
    
    public bool useParentColourAsDefault = false;
    public bool hasParentColourPaletteOption = false;
    
    public bool flipable = false;
    
    [Obsolete]
    public bool useMask = false;
    public MaskDef defaultMask;
    
    public DecorationTypeDef decorationType;
    
    public List<DecorationColourPresetDef> availablePresets = [];
    
    public bool isIncompatibleWithBaseTexture = false;
    public List<DecorationDef> incompatibleDecorations = [];
    
    public List<RankDef> mustHaveRank = null;
    public List<GeneDef> mustHaveGene = null;
    public List<TraitData> mustHaveTrait = null;
    public List<HediffDef> mustHaveHediff = null;
    
    public List<StatModifier> statOffsets = [];
    public List<StatModifier> statFactors = [];
    
    public List<AbilityDef> givesAbilities = [];
    public List<VEF.Abilities.AbilityDef> givesVFEAbilities = [];

    //Hediffs put on the wearer/holder while this is attached, and taken off again when it is
    //removed or the item is unequipped.
    public List<HediffDef> givesHediffs = [];

    //An internal upgrade changes what the item does without changing how it looks. Nothing is drawn
    //for it, it has no colours, mask or flip, and it takes up internal slots on the item.
    public bool isInternal = false;

    //How many of the item's internal slots this fills. Only read for internal upgrades.
    public int slotCost = 1;

    //One time resource cost to unlock this decoration on an individual item. Written like
    //ThingDef.costList, so either <li><thingDef>Steel</thingDef><count>30</count></li> or the
    //shorthand <Steel>30</Steel>. Once paid, the decoration stays unlocked on that item forever and
    //can be taken off and refitted without paying again.
    public List<ThingDefCountClass> cost = [];

    //Work to fit this decoration. Charged in full on every add, whether or not it is already
    //unlocked - resources are one time, labour is not.
    public float workAmount = 100f;

    //Removal work, as a fraction of workAmount.
    public float removalWorkFactor = 0.5f;

    //null means work it out from what the decoration actually does. Set explicitly to force a
    //decoration onto the Decoration tab or the Upgrades tab.
    public bool? isUpgrade = null;

    [Unsaved]
    private bool? isUpgradeCached;

    public bool IsUpgrade => isUpgradeCached ??= isUpgrade ?? AutoDetectUpgrade;

    //A decoration is an upgrade when it does something beyond looking nice. Note that `cost` is
    //deliberately not part of this - a purely cosmetic badge is allowed to cost steel and still
    //belong on the Decoration tab.
    protected virtual bool AutoDetectUpgrade =>
        isInternal
        || !statOffsets.NullOrEmpty()
        || !statFactors.NullOrEmpty()
        || !givesAbilities.NullOrEmpty()
        || !givesVFEAbilities.NullOrEmpty()
        || !givesHediffs.NullOrEmpty();

    public bool HasCost => !cost.NullOrEmpty();

    //Whether anything is drawn for this decoration. Internal upgrades never are, and a decoration
    //that somehow lost its texture path fails safe to drawing nothing rather than building a broken
    //render node.
    public bool HasVisual => !isInternal && !drawnTextureIconPath.NullOrEmpty();

    public float RemovalWork => workAmount * removalWorkFactor;

    public virtual string TooltipDescription()
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

        if (HasCost && DecorationWorkUtility.CostEnabled)
        {
            stringbuilder.AppendLine();
            stringbuilder.AppendLine("BEWH.Framework.Customization.Cost".Translate());
            foreach (var thingCount in cost)
            {
                stringbuilder.AppendLine("BEWH.Framework.Customization.CostLine".Translate(thingCount.thingDef.LabelCap, thingCount.count));
            }
        }

        if (isInternal && slotCost > 0)
        {
            stringbuilder.AppendLine();
            stringbuilder.AppendLine("BEWH.Framework.Customization.SlotCost".Translate(slotCost));
        }

        stringbuilder.AppendLine();
        stringbuilder.AppendLine("BEWH.Framework.Customization.WorkAmount".Translate(workAmount.ToString("F0"), RemovalWork.ToString("F0")));

        return stringbuilder.ToString();
    }
    
    public virtual bool HasRequirements(Pawn pawn, out string lockedReason)
    {
        var reason = new StringBuilder();
        var requirementFulfilled = true;

        //Nothing to check against. Anything with a requirement is locked, anything without is free.
        if (pawn == null)
        {
            lockedReason = string.Empty;
            return mustHaveRank == null && mustHaveGene == null && mustHaveTrait == null && mustHaveHediff == null;
        }

        if (mustHaveRank != null)
        {
            //No rank tracker on this pawn (CompRankInfo is only patched onto humans), so every
            //listed rank counts as missing. Returning here keeps the queries below off a null comp.
            var comp = pawn.GetComp<CompRankInfo>();
            if (comp == null)
            {
                reason.AppendLine("BEWH.Framework.Customization.MissingRanks".Translate());
                foreach (var rank in mustHaveRank)
                {
                    reason.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(rank.label.CapitalizeFirst()));
                }
                lockedReason = reason.ToString();
                return false;
            }
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
            //No gene tracker (Biotech off, or a race without one) means none of them are present.
            if (pawn.genes == null)
            {
                reason.AppendLine("BEWH.Framework.Customization.MissingGenes".Translate());
                foreach (var gene in mustHaveGene)
                {
                    reason.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(gene.label.CapitalizeFirst()));
                }
                lockedReason = reason.ToString();
                return false;
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
                reason.AppendLine("BEWH.Framework.Customization.MissingTraits".Translate());
                foreach (var trait in mustHaveTrait)
                {
                    reason.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(trait.traitDef.label.CapitalizeFirst()));
                }
                lockedReason = reason.ToString();
                return false;
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
                reason.AppendLine("BEWH.Framework.Customization.MissingHediffs".Translate());
                foreach (var hediff in mustHaveHediff)
                {
                    reason.AppendLine("BEWH.Framework.Customization.AppendedLabel".Translate(hediff.label.CapitalizeFirst()));
                }
                lockedReason = reason.ToString();
                return false;
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
    
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var configError in base.ConfigErrors())
        {
            yield return configError;
        }

        if (workAmount < 0f)
        {
            yield return "workAmount is negative";
        }

        if (removalWorkFactor < 0f)
        {
            yield return "removalWorkFactor is negative";
        }

        if (isInternal)
        {
            if (!drawnTextureIconPath.NullOrEmpty())
            {
                yield return "isInternal but has a drawnTextureIconPath - internal upgrades are never drawn";
            }
            if (isIncompatibleWithBaseTexture)
            {
                yield return "isInternal but isIncompatibleWithBaseTexture - nothing is drawn, so it cannot clash with a base texture";
            }
            if (slotCost < 0)
            {
                yield return "slotCost is negative";
            }
        }
        else if (drawnTextureIconPath.NullOrEmpty() && this is not AlternateBaseFormDef)
        {
            yield return "no drawnTextureIconPath and not marked isInternal - nothing will be drawn for this decoration";
        }

        if (cost.NullOrEmpty())
        {
            yield break;
        }

        foreach (var thingCount in cost)
        {
            if (thingCount.thingDef == null)
            {
                yield return "cost entry has no thingDef";
            }
            else if (thingCount.count <= 0)
            {
                yield return "cost entry for " + thingCount.thingDef.defName + " has a count of " + thingCount.count;
            }
        }
    }

    public override void ResolveReferences()
    {
        shaderType ??= Core40kDefOf.BEWH_CutoutThreeColor;
        defaultMask ??= Core40kDefOf.BEWH_DefaultMask;

        if (isInternal)
        {
            //Nothing is drawn, so the appearance options cannot mean anything. Forced rather than
            //only reported as a config error, so a stray XML value can never reach the drawing code.
            colourable = false;
            flipable = false;
            //Internal upgrades group under their own header in the Upgrades tab unless the content
            //explicitly picked a category.
            decorationType ??= Core40kDefOf.BEWH_DecoCategory_Internal;
        }

        decorationType ??= Core40kDefOf.BEWH_UndefinedType;
        if (useMask)
        {
            Log.Warning(defName + "has useMask set, this field is no longer needed and should be removed.");
        }
        base.ResolveReferences();
    }
}