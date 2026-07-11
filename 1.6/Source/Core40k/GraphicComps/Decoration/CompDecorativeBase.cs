using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class CompDecorativeBase : CompGraphicParent
{
    public CompMultiColor MultiColor => parent.GetComp<CompMultiColor>();
    
    public Dictionary<DecorationDef, DecorationDrawData> originalDrawDatas = new(); 
    public Dictionary<DecorationDef, DecorationDrawData> drawDatas = new(); 
    
    protected Dictionary<DecorationDef, DecorationSettings> originalDecorations = new ();
    protected Dictionary<DecorationDef, DecorationSettings> decorations = new ();
    
    public Dictionary<DecorationDef, DecorationSettings> Decorations => decorations ??= new Dictionary<DecorationDef, DecorationSettings>();
    
    //Add
    public virtual void ApplyDecorationsFromList(List<DecorationDef> list)
    {
        foreach (var extraDecoration in list)
        {
            AddOrRemoveDecoration(extraDecoration);
        }
    }
    public virtual void AddOrRemoveDecoration(DecorationDef decoration)
    {
        if (decorations.TryGetValue(decoration, out var setting))
        {
            if (decoration.flipable && !setting.Flipped)
            {
                setting.Flipped = true;
            }
            else
            {
                RemoveDecoration(decoration);
            }
        }
        else
        {
            AddDecoration(decoration, setDefaultColors: true);
        }
        Notify_GraphicChanged();
    }

    protected virtual void AddDecoration(DecorationDef decoration, DecorationSettings decorationSettings = null, bool setDefaultColors = false)
    {
        if (!decorations.ContainsKey(decoration))
        {
            decorations.Add(decoration, decorationSettings ?? new DecorationSettings());
            Pawn.AddAbilities(decoration.givesAbilities, decoration.givesVFEAbilities);
        }

        if (!drawDatas.ContainsKey(decoration))
        {
            drawDatas.Add(decoration, new DecorationDrawData());
        }

        if (setDefaultColors)
        {
            SetDefaultColors(decoration);
        }
    }
    
    //Remove
    protected virtual bool RemoveDecoration(DecorationDef decoration)
    {
        if (!decorations.Remove(decoration))
        {
            return false;
        }
        
        Pawn.RemoveAbilities(decoration.givesAbilities, decoration.givesVFEAbilities);
        drawDatas.Remove(decoration);
        
        return true;
    }
    public virtual void RemoveAllDecorations()
    {
        decorations = new Dictionary<DecorationDef, DecorationSettings>();
        drawDatas = new Dictionary<DecorationDef, DecorationDrawData>();
        Notify_GraphicChanged();
    }
    public virtual void RemoveInvalidDecorations(Pawn pawn)
    {
        decorations.RemoveAll(pair => !pair.Key.HasRequirements(pawn, out _));
        drawDatas.RemoveAll(pair => !pair.Key.HasRequirements(pawn, out _));
    }
    public virtual void RemoveDecorationsIncompatibleWithAlternate(AlternateBaseFormDef alternateBaseFormDef)
    {
        decorations.RemoveAll(pair => 
            (alternateBaseFormDef == null && pair.Key.isIncompatibleWithBaseTexture) || 
            (alternateBaseFormDef != null && alternateBaseFormDef.incompatibleDecorations.Contains(pair.Key)));
        drawDatas.RemoveAll(pair => 
            (alternateBaseFormDef == null && pair.Key.isIncompatibleWithBaseTexture) || 
            (alternateBaseFormDef != null && alternateBaseFormDef.incompatibleDecorations.Contains(pair.Key)));
    }
    
    
    //Decos Set
    public virtual void SetDecorationColourOne(DecorationDef decoration, Color colour)
    {
        decorations[decoration].Color = colour;
        Notify_GraphicChanged();
    }
    public virtual void SetDecorationColourTwo(DecorationDef decoration, Color colour)
    {
        decorations[decoration].ColorTwo = colour;
        Notify_GraphicChanged();
    }
    public virtual void SetDecorationColourThree(DecorationDef decoration, Color colour)
    {
        decorations[decoration].ColorThree = colour;
        Notify_GraphicChanged();
    }
    public void SetDecorationMask(DecorationDef decoration, MaskDef maskDef)
    {
        decorations[decoration].maskDef = maskDef;
        Notify_GraphicChanged();
    }
    public void SetDecorationToParentColors(DecorationDef decoration)
    {
        decorations[decoration].Color = MultiColor.DrawColor;
        decorations[decoration].ColorTwo = MultiColor.DrawColorTwo;
        decorations[decoration].ColorThree = MultiColor.DrawColorThree;
        Notify_GraphicChanged();
    }
    public virtual void SetDefaultColors(DecorationDef decoration, bool resetMaskDef = true)
    {
        decorations[decoration].Color = decoration.defaultColour ?? (decoration.useParentColourAsDefault ? MultiColor.DrawColor : Color.white);
        decorations[decoration].ColorTwo = decoration.defaultColourTwo ?? (decoration.useParentColourAsDefault ? MultiColor.DrawColorTwo : Color.white);
        decorations[decoration].ColorThree = decoration.defaultColourThree ?? (decoration.useParentColourAsDefault ? MultiColor.DrawColorThree : Color.white);
        if (resetMaskDef)
        {
            decorations[decoration].maskDef = decoration.defaultMask;
        }
        Notify_GraphicChanged();
    }
    public void SetDrawData(DecorationDef decoDef, DecorationDrawData drawData)
    {
        drawDatas[decoDef] = drawData;
        Notify_GraphicChanged();
    }
    
    //Preset Loads
    public virtual void LoadFromPreset(DecorationPreset preset)
    {
        foreach (var presetPart in preset.decorationPresetParts)
        {
            var decoDef = Core40kUtils.GetDecoDefFromString(presetPart.extraDecorationDefs);
            if (decorations.ContainsKey(decoDef))
            {
                continue;
            }
            var extraDecorationsSetting = new DecorationSettings()
            {
                Flipped = presetPart.flipped,
                Color = presetPart.colour,
                ColorTwo = presetPart.colourTwo,
                ColorThree = presetPart.colourThree,
                maskDef = presetPart.maskDef ?? Core40kDefOf.BEWH_DefaultMask,
            };
            
            AddDecoration(decoDef, extraDecorationsSetting);
        }
    }
    public void LoadFromPreset(DecorationPresetDef preset)
    {
        foreach (var presetPart in preset.presetData)
        {
            if (decorations.ContainsKey(presetPart.decorationDef))
            {
                continue;
            }
            var multiColComp = parent.GetComp<CompMultiColor>();
            var extraDecorationsSetting = new DecorationSettings()
            {
                Flipped = presetPart.flipped,
                Color = presetPart.colour ?? (presetPart.decorationDef.useParentColourAsDefault ? multiColComp?.DrawColor ?? parent.DrawColor : Color.white),
                ColorTwo = presetPart.colourTwo ?? (presetPart.decorationDef.useParentColourAsDefault ? multiColComp?.DrawColorTwo ?? parent.DrawColorTwo : Color.white),
                ColorThree = presetPart.colourThree ?? (presetPart.decorationDef.useParentColourAsDefault ? multiColComp?.DrawColorThree ?? parent.DrawColorTwo : Color.white),
                maskDef = presetPart.maskDef ?? Core40kDefOf.BEWH_DefaultMask,
            };
            
            AddDecoration(presetPart.decorationDef, extraDecorationsSetting);
        }
    }
    
    //Originals
    public override void SetOriginals()
    {
        SetOriginalDecorations();
        SetOriginalDrawDatas();
        base.SetOriginals();
    }
    public void SetOriginalDrawDatas()
    {   
        originalDrawDatas = new Dictionary<DecorationDef, DecorationDrawData>();
        foreach (var drawData in drawDatas)
        {
            var newDrawData = new DecorationDrawData();
            newDrawData.CopyFrom(drawData.Value);
            originalDrawDatas.Add(drawData.Key, newDrawData);
        }
    }
    public void SetOriginalDrawData(DecorationDef decoDef)
    {   
        originalDrawDatas.Remove(decoDef);
        var drawData = new DecorationDrawData();
        if (drawDatas.TryGetValue(decoDef, out var data))
        {
            drawData.CopyFrom(data);
        }
        originalDrawDatas.Add(decoDef, drawData);
    }
    public void SetOriginalDecorations()
    {
        originalDecorations = new Dictionary<DecorationDef, DecorationSettings>();
        originalDecorations.AddRange(decorations);
    }
    
    //Resets
    public override void Reset()
    {
        ResetDecorations();
        ResetDrawDatas();
        
        Notify_GraphicChanged();
        base.Reset();
    }
    public void ResetDrawDatas()
    {   
        drawDatas = new Dictionary<DecorationDef, DecorationDrawData>();
        drawDatas.AddRange(originalDrawDatas);
    }
    public void ResetDrawData(DecorationDef decoDef)
    {   
        drawDatas.Remove(decoDef);
        var drawData = new DecorationDrawData();
        if (originalDrawDatas.TryGetValue(decoDef, out var data))
        {
            drawData = data;
        }
        drawDatas.Add(decoDef, drawData);
        Notify_GraphicChanged();
    }
    public void ResetDrawData(DecorationDef decoDef, Rot4 rot4)
    {
        if (originalDrawDatas.TryGetValue(decoDef, out var data))
        {
            drawDatas[decoDef].GetData(rot4) = data.GetData(rot4).GetCopy();
        }
        else
        {
            drawDatas[decoDef].GetData(rot4) = new DecorationDrawData.RotationalData(rot4);
        }
        Notify_GraphicChanged();
    }
    public void ResetDecorations()
    {
        decorations = new Dictionary<DecorationDef, DecorationSettings>();
        decorations.AddRange(originalDecorations);
    }
    
    //DrawData for Rot
    public virtual Vector3 GetAdditionalOffsetForRot(Rot4 rot, DecorationDef decorationDef)
    {
        var offset = Vector3.zero;

        if (drawDatas.TryGetValue(decorationDef, out var data))
        {
            offset += data.GetData(rot).offset;
        }

        return offset;
    }
    public virtual float GetAdditionalLayerForRot(Rot4 rot, DecorationDef decorationDef)
    {
        var layer = 0f;
        
        if (drawDatas.TryGetValue(decorationDef, out var data))
        {
            layer += data.GetData(rot).layer;
        }

        return layer;
    }
    public virtual Vector3 GetAdditionalScaleForRot(Rot4 rot, DecorationDef decorationDef)
    {
        var scale = Vector3.one;
        
        if (drawDatas.TryGetValue(decorationDef, out var data))
        {
            scale *= data.GetData(rot).scale;
        }

        return scale;
    }
    
    //Notifi's
    public override void Notify_Equipped(Pawn pawn)
    {
        RemoveInvalidDecorations(pawn);
        
        TryAddCachedStat(pawn);
        
        Notify_GraphicChanged();
        base.Notify_Equipped(pawn);
    }
    public override void Notify_Unequipped(Pawn pawn)
    {
        if (pawn != null)
        {
            if (CoreUtils.cachedDecoratives.TryGetValue(pawn, out var decoratives))
            {
                if (parent is Apparel apparel)
                {
                    decoratives.apparels.Remove(apparel);
                }
                else
                {
                    decoratives.weapon = null;
                }
                
                cachedStatOffset = new Dictionary<StatDef, float>();
                cachedStatFactor = new Dictionary<StatDef, float>();
            }
        }
        
        base.Notify_Unequipped(pawn);
    }
    private void TryAddCachedStat(Pawn pawn)
    {
        if (pawn != null)
        {
            cachedStatOffset = new Dictionary<StatDef, float>();
            cachedStatFactor = new Dictionary<StatDef, float>();
            
            if (CoreUtils.cachedDecoratives.TryGetValue(pawn, out var decoratives))
            {
                if (parent is Apparel apparel)
                {
                    decoratives.apparels.Add(apparel);
                }
                else
                {
                    decoratives.weapon = parent;
                }
                
            }
            else
            {
                GameComponent_CoreUtils.CachedDecoratives cachedDecoratives;
                if (parent is Apparel apparel)
                {
                    cachedDecoratives = new GameComponent_CoreUtils.CachedDecoratives
                    {
                        apparels = [apparel],
                    };
                }
                else
                {
                    cachedDecoratives = new GameComponent_CoreUtils.CachedDecoratives
                    {
                        apparels = [],
                        weapon = parent,
                    };
                }

                CoreUtils.cachedDecoratives.Add(pawn, cachedDecoratives);
            }
        }
    }
    
    //Stat Related
    public override float GetStatOffset(StatDef stat)
    {
        var num = 0f;
        if (CachedStatOffset == null || stat == null)
        {
            return num;
        }
        if (CachedStatOffset.TryGetValue(stat, out var cachedStatOffsetOut))
        {
            num += cachedStatOffsetOut;
        }
        else
        {
            var resNum = 0f;
            foreach (var decoration in Decorations)
            {
                if (decoration.Key?.statOffsets == null)
                {   
                    continue;
                }
                if (!decoration.Key.statOffsets.NullOrEmpty())
                {
                    resNum += decoration.Key.statOffsets.GetStatOffsetFromList(stat);
                }
            }
            CachedStatOffset.Add(stat, resNum);
            num += resNum;
        }
        return num;
    }
    public override float GetStatFactor(StatDef stat)
    {
        var num = 1f;
        if (CachedStatFactor == null || stat == null)
        {
            return num;
        }
        if (CachedStatFactor.TryGetValue(stat, out var cachedStatFactorOut))
        {
            num *= cachedStatFactorOut;
        }
        else
        {
            var resNum = 1f;
                    
            foreach (var decoration in Decorations)
            {
                if (decoration.Key?.statFactors == null)
                {
                    continue;
                }
                if (!decoration.Key.statFactors.NullOrEmpty())
                {
                    resNum *= decoration.Key.statFactors.GetStatFactorFromList(stat);
                }
            }
                    
            CachedStatFactor.Add(stat, resNum);
            num *= resNum;
        }
        
        return num;
    }
    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
    {
        if (Decorations.NullOrEmpty())
        {
            base.GetStatsExplanation(stat, sb, whitespace);
            return;
        }
        var stringBuilder = new StringBuilder();
        
        foreach (var decoration in Decorations)
        {
            var statOffsetFromList = decoration.Key.statOffsets.GetStatOffsetFromList(stat);
            if (!Mathf.Approximately(statOffsetFromList, 0f))
            {
                stringBuilder.AppendLine(whitespace + "    " + decoration.Key.LabelCap + ": " + stat.Worker.ValueToString(statOffsetFromList, finalized: false, ToStringNumberSense.Offset));
            }
            var statFactorFromList = decoration.Key.statFactors.GetStatFactorFromList(stat);
            if (!Mathf.Approximately(statFactorFromList, 1f))
            {
                stringBuilder.AppendLine(whitespace + "    " + decoration.Key.LabelCap + ": " + stat.Worker.ValueToString(statFactorFromList, finalized: false, ToStringNumberSense.Factor));
            }
        }
        
        if (stringBuilder.Length != 0)
        {
            sb.AppendLine(whitespace + "BEWH.Framework.StatReport.Decoration".Translate() + ":");
            sb.Append(stringBuilder);
        }
    }
    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (var pair in GetStatOffsetsFromDecorations())
        {
            var val = pair.Value.Sum(modifier => modifier.value);
            yield return new StatDrawEntry(Core40kDefOf.BEWH_DecorationOffsets, pair.Key, pair.Key.Worker.ValueToString(val, finalized: false, ToStringNumberSense.Offset));
        }
        
        foreach (var pair in GetStatFactorsFromDecorations())
        {
            var val = pair.Value.Sum(modifier => modifier.value);
            yield return new StatDrawEntry(Core40kDefOf.BEWH_DecorationFactors, pair.Key, pair.Key.Worker.ValueToString(val, finalized: false, ToStringNumberSense.Factor));
        }
    }
    private Dictionary<StatDef, List<StatModifier>> GetStatOffsetsFromDecorations()
    {
        var dict = new  Dictionary<StatDef, List<StatModifier>>();
        foreach (var decoration in decorations)
        {
            foreach (var statModifier in decoration.Key.statOffsets)
            {
                if (dict.ContainsKey(statModifier.stat))
                {
                    dict[statModifier.stat].Add(statModifier);
                }
                else
                {
                    dict.Add(statModifier.stat, [statModifier]);
                }
            }
        }

        return dict;
    }
    private Dictionary<StatDef, List<StatModifier>> GetStatFactorsFromDecorations()
    {
        var dict = new  Dictionary<StatDef, List<StatModifier>>();
        foreach (var decoration in decorations)
        {
            foreach (var statModifier in decoration.Key.statFactors)
            {
                if (dict.ContainsKey(statModifier.stat))
                {
                    dict[statModifier.stat].Add(statModifier);
                }
                else
                {
                    dict.Add(statModifier.stat, [statModifier]);
                }
            }
        }

        return dict;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        
        Scribe_Collections.Look(ref decorations, "decorations");
        
        Scribe_Collections.Look(ref drawDatas, "drawData");
        
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        drawDatas ??= new Dictionary<DecorationDef, DecorationDrawData>();
        TryAddCachedStat(Wearer);
    }
}