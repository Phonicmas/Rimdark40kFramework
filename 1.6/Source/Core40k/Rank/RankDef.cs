using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;
using AbilityDef = RimWorld.AbilityDef;

namespace Core40k;

public class RankDef : Def
{
    [NoTranslate]
    public string rankIconPath;
        
    public List<RankDef> incompatibleRanks = [];
    public List<Aptitude> requiredSkills = [];
        
    public List<GeneDef> requiredGenesAll = [];
    public List<GeneDef> requiredGenesOneAmong = [];
        
    public List<TraitData> requiredTraitsAll = [];
    public List<TraitData> requiredTraitsOneAmong = [];
        
    public List<StatModifier> statOffsets = [];
    public List<StatModifier> statFactors = [];
        
    public List<AbilityDef> givesAbilities = [];
    public List<VEF.Abilities.AbilityDef> givesVFEAbilities = [];
    
    public List<HediffData> givesHediffs = [];
    
    public List<SkillDef> recreationFromSkills = [];
    
    public List<PassionMod> givesPassions = [];
        
    public List<string> customEffectDescriptions = [];

    public List<RankDef> removeRanksOnUnlock = [];
        
    public Vector2 colonyLimitOfRank = new Vector2(-1, -1);

    public bool defaultFirstRank = false;

    public int rankTier = 0;
        
    public bool specialistRank = false;

    public string newPawnCardTitle = string.Empty;
        
    [Unsaved]
    private Texture2D rankIcon;

    public Texture2D RankIcon
    {
        get
        {
            if (rankIcon != null)
            {
                return rankIcon;
            }
                
            rankIcon = !rankIconPath.NullOrEmpty() ? ContentFinder<Texture2D>.Get(rankIconPath) : BaseContent.BadTex;
            return rankIcon;
        }
    }

    private static GameComponent_RankInfo gameCompRankInfo = null;
    private static GameComponent_RankInfo GameCompRankInfo => gameCompRankInfo ??= Current.Game.GetComponent<GameComponent_RankInfo>();
    
    public virtual void UnlockRank(CompRankInfo rankComp)
    {
        rankComp.ParentPawn.AddAbilities(givesAbilities, givesVFEAbilities);

        if (!givesPassions.NullOrEmpty())
        {
            if (rankComp.originalPassions.NullOrEmpty())
            {
                foreach (var passion in givesPassions)
                {
                    var skill = rankComp.ParentPawn.skills.GetSkill(passion.skill);
                    if (!rankComp.originalPassions.ContainsKey(skill.def))
                    {
                        rankComp.originalPassions.Add(skill.def, skill.passion);
                        skill.passion = passion.NewPassionFor(skill);
                    }
                }
            }
            rankComp.RecalculatePassions();
        }

        if (!givesHediffs.NullOrEmpty())
        {
            foreach (var hediffData in givesHediffs)
            {
                var hediff = HediffMaker.MakeHediff(hediffData.hediffDef, rankComp.ParentPawn, rankComp.ParentPawn.health.hediffSet.GetBodyPartRecord(hediffData.bodyPartDef));
                hediff.Severity = hediffData.initialSeverity;
                rankComp.ParentPawn.health.AddHediff(hediff);
            }
        }

        if (!removeRanksOnUnlock.NullOrEmpty())
        {
            foreach (var rankDef in removeRanksOnUnlock)
            {
                rankComp.RemoveRank(rankDef, true);
            }
        }
    }

    public virtual void RemoveRank(CompRankInfo rankComp)
    {
        rankComp.ParentPawn.RemoveAbilities(givesAbilities, givesVFEAbilities);
        
        if (givesPassions != null)
        {
            if (!rankComp.originalPassions.NullOrEmpty())
            {
                rankComp.RecalculatePassions();
            }
        }
        
        if (givesHediffs != null)
        {
            foreach (var hediff in givesHediffs)
            {
                var hediffOfDef = rankComp.ParentPawn.health.hediffSet.GetFirstHediffOfDef(hediff.hediffDef);
                rankComp.ParentPawn.health.RemoveHediff(hediffOfDef);
            }
        }
    }

    public virtual bool RequirementMet(StringBuilder stringBuilder, Pawn pawn, CompRankInfo rankComp, RankCategoryDef currentlySelectedRankCategory, out string reason)
    {
        //Limit on rank amount
        var rankLimitRequirementsMet = true;
        if (colonyLimitOfRank.x > 0 || (colonyLimitOfRank.x == 0 && colonyLimitOfRank.y > 0))
        {
            var (allowed, allowedAmount, currentAmount) = GameCompRankInfo.CanHaveMoreOfRankWithInfo(this);
            rankLimitRequirementsMet = allowed;
                
            var requirementColour = rankLimitRequirementsMet ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                
            stringBuilder.Append("\n");
            if (colonyLimitOfRank.y == 0)
            {
                stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsLimitOnlyOneEver".Translate(allowedAmount, currentAmount).Colorize(requirementColour));
            }
            else
            {
                var limitIncreaseAmount = colonyLimitOfRank.y;
                var text = " " + limitIncreaseAmount;
                switch (limitIncreaseAmount)
                {
                    case 1:
                        text = "";
                        break;
                    case 2:
                        text += "nd";
                        break;
                    case 3:
                        text += "rd";
                        break;
                    default:
                        text += "th";
                        break;
                }
                stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsLimit".Translate(allowedAmount, currentAmount, text).Colorize(requirementColour));
            }
        }
            
        //Skills
        var skillRequirementsMet = true;
        if (!requiredSkills.NullOrEmpty())
        {
            stringBuilder.Append("\n");
            stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsSkills".Translate());
            foreach (var aptitude in requiredSkills)
            {
                var skillRequirementMet = pawn.skills.GetSkill(aptitude.skill).Level >= aptitude.level;
                if (!skillRequirementMet)
                {
                    skillRequirementsMet = false;
                }
                var requirementColour = skillRequirementMet ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                stringBuilder.AppendLine(("    " + aptitude.skill.label.CapitalizeFirst() + ": " + aptitude.level).Colorize(requirementColour));
            }
        }
            
        //Ranks
        //All
        var rankAllRequirementsMet = true;
        if (!currentlySelectedRankCategory.rankDict[this].rankRequirements.NullOrEmpty())
        {
            stringBuilder.Append("\n");
            stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsRanks".Translate());
            foreach (var rank in currentlySelectedRankCategory.rankDict[this].rankRequirements)
            {
                var requiredDaysAsRank = Math.Round(rank.daysAs / pawn.GetStatValue(Core40kDefOf.BEWH_RankLearningFactor));
                var rankRequirementMet = rankComp.HasRank(rank.rankDef) &&
                                         rankComp.GetDaysAsRank(rank.rankDef) >= requiredDaysAsRank;
                    
                if (!rankRequirementMet)
                {
                    rankAllRequirementsMet = false;
                }
                    
                var requirementColour = rankRequirementMet ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                    
                if (rank.daysAs > 0)
                {
                    stringBuilder.AppendLine(("    " + "BEWH.Framework.RankSystem.HaveBeenRankForDays".Translate(rank.rankDef.label.CapitalizeFirst(), requiredDaysAsRank)).Colorize(requirementColour));
                }
                else
                {
                    stringBuilder.AppendLine(("    " + "BEWH.Framework.RankSystem.HaveAchievedRank".Translate(rank.rankDef.label.CapitalizeFirst())).Colorize(requirementColour));
                }
                    
            }
        }
        //One Among
        var rankAtLeastOneRequirementsMet = currentlySelectedRankCategory.rankDict[this].rankRequirementsOneAmong.NullOrEmpty();
        if (!rankAtLeastOneRequirementsMet)
        {
            stringBuilder.Append("\n");
            stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsRanksAtLeastOne".Translate());
            foreach (var rank in currentlySelectedRankCategory.rankDict[this].rankRequirementsOneAmong)
            {
                var rankRequirementMet = rankComp.HasRank(rank.rankDef) &&
                                         rankComp.GetDaysAsRank(rank.rankDef) >= rank.daysAs / pawn.GetStatValue(Core40kDefOf.BEWH_RankLearningFactor);
                    
                if (rankRequirementMet)
                {
                    rankAtLeastOneRequirementsMet = true;
                }
                    
                var requirementColour = rankRequirementMet ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                    
                if (rank.daysAs > 0)
                {
                    stringBuilder.AppendLine(("    " + "BEWH.Framework.RankSystem.HaveBeenRankForDays".Translate(rank.rankDef.label.CapitalizeFirst(), rank.daysAs)).Colorize(requirementColour));
                }
                else
                {
                    stringBuilder.AppendLine(("    " + "BEWH.Framework.RankSystem.HaveAchievedRank".Translate(rank.rankDef.label.CapitalizeFirst())).Colorize(requirementColour));
                }
            }
        }
            
        //Traits
        //All
        var traitsAllRequirementsMet = true;
        if (!requiredTraitsAll.NullOrEmpty())
        {
            stringBuilder.Append("\n");
            stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsTraitAll".Translate());
            foreach (var trait in requiredTraitsAll)
            {
                var traitsAllRequirementMet = pawn.story.traits.HasTrait(trait.traitDef, trait.degree);
                    
                if (!traitsAllRequirementMet)
                {
                    traitsAllRequirementsMet = false;
                }
                    
                var requirementColour = traitsAllRequirementMet ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                stringBuilder.AppendLine(("    " + trait.traitDef.DataAtDegree(trait.degree).label.CapitalizeFirst()).Colorize(requirementColour));
            }
        }
        //One Among
        var traitsAtLeastOneRequirementsMet = requiredTraitsOneAmong.NullOrEmpty();
        if (!traitsAtLeastOneRequirementsMet)
        {
            stringBuilder.Append("\n");
            stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsTraitAtLeastOne".Translate());
            foreach (var trait in requiredTraitsOneAmong)
            {
                var traitsAtLeastOneRequirementMet = pawn.story.traits.HasTrait(trait.traitDef, trait.degree);
                        
                if (traitsAtLeastOneRequirementMet)
                {
                    traitsAtLeastOneRequirementsMet = true;
                }
                    
                var requirementColour = traitsAtLeastOneRequirementMet ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                    
                stringBuilder.AppendLine(("    " + trait.traitDef.DataAtDegree(trait.degree).label.CapitalizeFirst()).Colorize(requirementColour));
            }
        }
            
        //Genes
        var genesAllRequirementsMet = true;
        var genesAtLeastOneRequirementsMet = requiredGenesOneAmong.NullOrEmpty();
        if (pawn.genes != null)
        {
            //All
            if (!requiredGenesAll.NullOrEmpty())
            {
                stringBuilder.Append("\n");
                stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsGeneAll".Translate());
                foreach (var gene in requiredGenesAll)
                {
                    var genesAllRequirementMet = pawn.genes.HasActiveGene(gene);
                        
                    if (!genesAllRequirementMet)
                    {
                        genesAllRequirementsMet = false;
                    }
                        
                    var requirementColour = genesAllRequirementMet ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                        
                    stringBuilder.AppendLine(("    " + gene.label.CapitalizeFirst()).Colorize(requirementColour));
                }
            }
            //One Among
            if (!genesAtLeastOneRequirementsMet)
            {
                stringBuilder.Append("\n");
                stringBuilder.AppendLine("BEWH.Framework.RankSystem.RequirementsGeneAtLeastOne".Translate());
                foreach (var gene in requiredGenesOneAmong)
                {
                    var genesAtLeastOneRequirementMet = pawn.genes.HasActiveGene(gene);
                        
                    if (genesAtLeastOneRequirementMet)
                    {
                        genesAtLeastOneRequirementsMet = true;
                    }
                    
                    var requirementColour = genesAtLeastOneRequirementMet ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                    stringBuilder.AppendLine(("    " + gene.label.CapitalizeFirst()).Colorize(requirementColour));
                }
            }
        }
        //Incompatible Ranks
        var noIncompatibleRanks = true;
        if (!incompatibleRanks.NullOrEmpty())
        {
            stringBuilder.Append("\n");
            stringBuilder.AppendLine("BEWH.Framework.RankSystem.IncompatibleRank".Translate());
            foreach (var rank in incompatibleRanks)
            {
                var isIncompatibleRank = rankComp.HasRank(rank);

                if (isIncompatibleRank)
                {
                    noIncompatibleRanks = false;
                }
                    
                var requirementColour = !isIncompatibleRank ? Core40kUtils.RequirementMetColour : Core40kUtils.RequirementNotMetColour;
                    
                stringBuilder.AppendLine(("    " + rank.label.CapitalizeFirst()).Colorize(requirementColour));
            }
        }
            
        var requirementText = stringBuilder.ToString().TrimEnd('\r', '\n').TrimStart('\r', '\n');
        if (requirementText.NullOrEmpty())
        {
            requirementText = "    " + "BEWH.Framework.CommonKeyword.None".Translate();
        }

        var requirementsMet = skillRequirementsMet 
                              && rankAllRequirementsMet 
                              && rankAtLeastOneRequirementsMet
                              && rankLimitRequirementsMet 
                              && noIncompatibleRanks 
                              && traitsAllRequirementsMet 
                              && traitsAtLeastOneRequirementsMet 
                              && genesAllRequirementsMet 
                              && genesAtLeastOneRequirementsMet;

        reason = requirementText;
        return requirementsMet;
    }

    public virtual string BuildRankBonusString(StringBuilder stringBuilder)
    {
        //Stats
        foreach (var statOffset in statOffsets)
        {
            stringBuilder.AppendLine("    " + statOffset.stat.label.CapitalizeFirst() + ": " + Core40kUtils.ValueToString(statOffset.stat, statOffset.value, finalized: false, ToStringNumberSense.Offset));
        }
        foreach (var statFactor in statFactors)
        {
            stringBuilder.AppendLine("    " + statFactor.stat.label.CapitalizeFirst() + ": " + Core40kUtils.ValueToString(statFactor.stat, statFactor.value, finalized: false, ToStringNumberSense.Factor));
        }
            
        var statBonuses = stringBuilder.ToString();
        if (!statBonuses.NullOrEmpty())
        {
            statBonuses = "BEWH.Framework.RankSystem.Stats".Translate() + "\n" + statBonuses;
        }
            
        //Abilities
        var abilityStringBuilder = new StringBuilder();
        foreach (var ability in givesAbilities)
        {
            abilityStringBuilder.AppendLine("    " + ability.label.CapitalizeFirst());
        }
        foreach (var abilityVfe in givesVFEAbilities)
        {
            abilityStringBuilder.AppendLine("    " + abilityVfe.label.CapitalizeFirst());
        }
        
        var abilityBonuses = abilityStringBuilder.ToString();
        if (!abilityBonuses.NullOrEmpty())
        {
            abilityBonuses = "BEWH.Framework.RankSystem.Abilities".Translate() + "\n" + abilityBonuses;
        }
        
        //Hediffs
        var hediffStringBuilder = new StringBuilder();
        foreach (var hediff in givesHediffs)
        {
            hediffStringBuilder.AppendLine("    " + hediff.hediffDef.label.CapitalizeFirst());
        }
        
        var hediffBonuses = hediffStringBuilder.ToString();
        if (!hediffBonuses.NullOrEmpty())
        {
            hediffBonuses = "BEWH.Framework.RankSystem.Hediffs".Translate() + "\n" + hediffBonuses;
        }
        
        //Recreation From Skills
        var recreationFromSkillStringBuilder = new StringBuilder();
        foreach (var recreationFromSkill in recreationFromSkills)
        {
            recreationFromSkillStringBuilder.AppendLine("    " + recreationFromSkill.label.CapitalizeFirst());
        }
        
        var recreationFromSkillBonuses = recreationFromSkillStringBuilder.ToString();
        if (!recreationFromSkillBonuses.NullOrEmpty())
        {
            recreationFromSkillBonuses = "BEWH.Framework.RankSystem.RecreationFromSkill".Translate() + "\n" + recreationFromSkillBonuses;
        }
        
        //Passions
        var givesPassionsStringBuilder = new StringBuilder();
        foreach (var passionMod in givesPassions)
        {
            var passionText = passionMod.skill.label.CapitalizeFirst();
            switch (passionMod.modType)
            {
                case PassionMod.PassionModType.AddOneLevel:
                    passionText += "BEWH.Framework.RankSystem.PassionModAddOne".Translate();
                    break;
                case PassionMod.PassionModType.DropAll:
                    passionText += "BEWH.Framework.RankSystem.PassionModDropAll".Translate();
                    break;
            }
            givesPassionsStringBuilder.AppendLine("    " + passionText);
        }
        
        var givesPassionsBonuses = givesPassionsStringBuilder.ToString();
        if (!givesPassionsBonuses.NullOrEmpty())
        {
            givesPassionsBonuses = "BEWH.Framework.RankSystem.PassionMod".Translate() + "\n" + givesPassionsBonuses;
        }
        
        //Custom Effects
        var customEffectStringBuilder = new StringBuilder();
        foreach (var customEffect in customEffectDescriptions)
        {
            customEffectStringBuilder.Append("    " + customEffect.CapitalizeFirst());
        }

        var customEffects = customEffectStringBuilder.ToString();
        if (!customEffects.NullOrEmpty())
        {
            customEffects = "BEWH.Framework.RankSystem.OtherEffects".Translate() + "\n" + customEffects;
        }

        //Final Result
        var result = "";
        if (!statBonuses.NullOrEmpty())
        {
            result = statBonuses;
        }

        if (!abilityBonuses.NullOrEmpty())
        {
            if (result.NullOrEmpty())
            {
                result = abilityBonuses;
            }
            else
            {
                result += "\n" + abilityBonuses;
            }
        }
        
        if (!hediffBonuses.NullOrEmpty())
        {
            if (result.NullOrEmpty())
            {
                result = hediffBonuses;
            }
            else
            {
                result += "\n" + hediffBonuses;
            }
        }

        if (!recreationFromSkillBonuses.NullOrEmpty())
        {
            if (result.NullOrEmpty())
            {
                result = recreationFromSkillBonuses;
            }
            else
            {
                result += "\n" + recreationFromSkillBonuses;
            }
        }
        
        if (!givesPassionsBonuses.NullOrEmpty())
        {
            if (result.NullOrEmpty())
            {
                result = givesPassionsBonuses;
            }
            else
            {
                result += "\n" + givesPassionsBonuses;
            }
        }
        
        if (!customEffects.NullOrEmpty())
        {
            if (result.NullOrEmpty())
            {
                result = customEffects;
            }
            else
            {
                result += "\n" + customEffects;
            }
        }

        return result;
    }

    public string GetRankBonusString()
    {
        var bonuses = BuildRankBonusString(new StringBuilder());
        if (bonuses.NullOrEmpty())
        {
            return"    " + "BEWH.Framework.CommonKeyword.None".Translate();
        }

        return bonuses;
    }
    
    public virtual void Notify_Killed(CompRankInfo rankComp, Map prevMap, DamageInfo? dinfo = null)
    {
        
    }
}