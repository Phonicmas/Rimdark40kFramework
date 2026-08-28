using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class CompAlternateTexture : CompGraphicParent
{
    private CompMultiColor MultiColor => parent?.GetComp<CompMultiColor>();
    
    private AlternateBaseFormDef originalCurrentAlternateBaseForm = null;
    private AlternateBaseFormDef currentAlternateBaseForm = null;
    public AlternateBaseFormDef CurrentAlternateBaseForm => currentAlternateBaseForm;

    public void SetAlternateBaseForm(AlternateBaseFormDef alternateBaseFormDef, bool isForApparel)
    {
        if (isForApparel)
        {
            var compArmorDeco = Thing?.TryGetComp<CompDecorative>();
            compArmorDeco?.RemoveDecorationsIncompatibleWithAlternate(alternateBaseFormDef);
        }
        else
        {
            var compWeaponDeco = Thing?.TryGetComp<CompWeaponDecoration>();
            compWeaponDeco?.RemoveDecorationsIncompatibleWithAlternate(alternateBaseFormDef);
        }
        
        if (MultiColor != null)
        {
            if (alternateBaseFormDef != null)
            {
                MultiColor?.ResetFieldsByAlternateTexture(alternateBaseFormDef);
            }
            else
            {
                MultiColor?.SetDefaultColors(); 
            }
        }

        if (currentAlternateBaseForm != null)
        {
            //Remove stuff from old alternate
            Pawn.RemoveAbilities(currentAlternateBaseForm.givesAbilities, currentAlternateBaseForm.givesVFEAbilities);
        }
        
        currentAlternateBaseForm = alternateBaseFormDef;
        
        if (currentAlternateBaseForm != null)
        {
            Pawn.AddAbilities(currentAlternateBaseForm.givesAbilities, currentAlternateBaseForm.givesVFEAbilities);
        }
        
        Notify_GraphicChanged();
    }
    
    
    private bool recacheMultiGraphics = true;
    public bool RecacheMultiGraphics => recacheMultiGraphics;
    private Graphic_Multi cachedGraphicMulti;
    public Graphic_Multi CachedGraphicMulti
    {
        get => cachedGraphicMulti;
        set
        {
            cachedGraphicMulti = value;
            recacheMultiGraphics = false;
            if (IsApparel)
            {
                apparelGraphicRecord = new ApparelGraphicRecord(cachedGraphicMulti, parent as Apparel);
            }
        }
    }
    private ApparelGraphicRecord? apparelGraphicRecord;
    public ApparelGraphicRecord ApparelGraphicRecord
    {
        get
        {
            if (!IsApparel)
            {
                return new ApparelGraphicRecord(null, null);
            }
            apparelGraphicRecord ??= new ApparelGraphicRecord(CachedGraphicMulti, parent as Apparel);
            return apparelGraphicRecord.Value;
        }
    }
    
    private bool recacheSingleGraphics = true;
    public bool RecacheSingleGraphics => recacheSingleGraphics;
    private Graphic cachedGraphic;
    private Graphic cachedDefaultGraphic;
    public Graphic GetSingleGraphic(bool onlyDefaultGraphic = false)
    {
        if (onlyDefaultGraphic)
        {
            if (cachedDefaultGraphic != null)
            {
                return cachedDefaultGraphic;
            }
        }
        else
        {
            if (cachedGraphic != null)
            {
                return cachedGraphic;
            }
        }
        
        SetSingleGraphic(onlyDefaultGraphic);
        return GetSingleGraphic(onlyDefaultGraphic);
    }
    public void SetSingleGraphic(bool onlyDefaultGraphic = false)
    {
        recacheSingleGraphics = false;
        var path = onlyDefaultGraphic ? ThingDef.graphicData.texPath : CurrentAlternateBaseForm?.drawnTextureIconPath ?? ThingDef.graphicData.texPath;
        var drawSize = CurrentAlternateBaseForm?.newDrawSize ?? ThingDef.graphicData.drawSize;
        var shader = Core40kDefOf.BEWH_CutoutThreeColor.Shader;
        var drawMult = IsApparel ? 0.9f : 1f;
        var graphic = MultiColorUtils.GetGraphic<Graphic_Single>(path, shader, drawSize*drawMult, MultiColor?.DrawColor ?? parent.DrawColor, MultiColor?.DrawColorTwo ?? parent.DrawColorTwo, MultiColor?.DrawColorThree ?? parent.DrawColorTwo, null, MultiColor?.MaskDef?.maskPath);
        if (onlyDefaultGraphic)
        {
            cachedDefaultGraphic = new Graphic_RandomRotated(graphic, 35f);
        }
        else
        {
            cachedGraphic = new Graphic_RandomRotated(graphic, 35f);
        }
    }
    
    
    public override void InitialSetup()
    {
        base.InitialSetup();
        recacheMultiGraphics = true;
    }
    
    public override void Notify_GraphicChanged()
    {
        recacheMultiGraphics = true;
        recacheSingleGraphics = true;
        base.Notify_GraphicChanged();
    }
    
    public override void Notify_Equipped(Pawn pawn)
    {
        TryAddCachedStat(pawn);

        Notify_GraphicChanged();
        base.Notify_Equipped(pawn);
    }
    
    public override void Notify_Unequipped(Pawn pawn)
    {
        if (pawn != null)
        {
            if (CoreUtils.cachedAlternateTexture.TryGetValue(pawn, out var alternateTexture))
            {
                if (parent is Apparel apparel)
                {
                    alternateTexture.apparels.Remove(apparel);
                }
                else
                {
                    alternateTexture.weapon = null;
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

            if (CoreUtils.cachedAlternateTexture.TryGetValue(pawn, out var alternateTexture))
            {
                if (parent is Apparel apparel)
                {
                    alternateTexture.apparels.Add(apparel);
                }
                else
                {
                    alternateTexture.weapon = parent;
                }

            }
            else
            {
                GameComponent_CoreUtils.CachedDecoratives cachedAlternateTexture;
                if (parent is Apparel apparel)
                {
                    cachedAlternateTexture = new GameComponent_CoreUtils.CachedDecoratives
                    {
                        apparels = [apparel],
                    };
                }
                else
                {
                    cachedAlternateTexture = new GameComponent_CoreUtils.CachedDecoratives
                    {
                        apparels = [],
                        weapon = parent,
                    };
                }

                CoreUtils.cachedAlternateTexture.Add(pawn, cachedAlternateTexture);
            }
        }
    }
    
    public override void SetOriginals()
    {
        originalCurrentAlternateBaseForm = currentAlternateBaseForm;
        Notify_GraphicChanged();
    }
    public override void Reset()
    {
        currentAlternateBaseForm = originalCurrentAlternateBaseForm;
        Notify_GraphicChanged();
    }
    
    //Stat Related
    public override float GetStatOffset(StatDef stat)
    {
        var num = 0f;

        if (CurrentAlternateBaseForm == null)
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
            
            resNum += CurrentAlternateBaseForm.statOffsets.GetStatOffsetFromList(stat);

            CachedStatOffset.Add(stat, resNum);
            num += resNum;
        }
        
        return num;
    }
    public override float GetStatFactor(StatDef stat)
    {
        var num = 1f;
        
        if (CurrentAlternateBaseForm == null)
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
                    
            resNum *= CurrentAlternateBaseForm.statFactors.GetStatFactorFromList(stat);
                    
            CachedStatFactor.Add(stat, resNum);
            num *= resNum;
        }
        
        return num;
    }
    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
    {
        if (CurrentAlternateBaseForm == null)
        {
            base.GetStatsExplanation(stat, sb, whitespace);
            return;
        }
        var stringBuilder = new StringBuilder();
        
        var statOffsetFromList = CurrentAlternateBaseForm.statOffsets.GetStatOffsetFromList(stat);
        if (!Mathf.Approximately(statOffsetFromList, 0f))
        {
            stringBuilder.AppendLine(whitespace + "    " + CurrentAlternateBaseForm.LabelCap + ": " + stat.Worker.ValueToString(statOffsetFromList, finalized: false, ToStringNumberSense.Offset));
        }
        var statFactorFromList = CurrentAlternateBaseForm.statFactors.GetStatFactorFromList(stat);
        if (!Mathf.Approximately(statFactorFromList, 1f))
        {
            stringBuilder.AppendLine(whitespace + "    " + CurrentAlternateBaseForm.LabelCap + ": " + stat.Worker.ValueToString(statFactorFromList, finalized: false, ToStringNumberSense.Factor));
        }
        
        if (stringBuilder.Length != 0)
        {
            sb.AppendLine(whitespace + "BEWH.Framework.StatReport.AlternateTexture".Translate() + ":");
            sb.Append(stringBuilder);
        }
    }
    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (CurrentAlternateBaseForm == null)
        {
            yield break;
        }
        
        foreach (var pair in GetStatOffsetsFromDecorations())
        {
            var val = pair.Value.Sum(modifier => modifier.value);
            yield return new StatDrawEntry(Core40kDefOf.BEWH_AlternateTextureOffsets, pair.Key, pair.Key.Worker.ValueToString(val, finalized: false, ToStringNumberSense.Offset));
        }
        
        foreach (var pair in GetStatFactorsFromDecorations())
        {
            var val = pair.Value.Sum(modifier => modifier.value);
            yield return new StatDrawEntry(Core40kDefOf.BEWH_AlternateTextureFactors, pair.Key, pair.Key.Worker.ValueToString(val, finalized: false, ToStringNumberSense.Factor));
        }
    }
    private Dictionary<StatDef, List<StatModifier>> GetStatOffsetsFromDecorations()
    {
        var dict = new  Dictionary<StatDef, List<StatModifier>>();
        foreach (var statModifier in CurrentAlternateBaseForm.statOffsets)
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

        return dict;
    }
    private Dictionary<StatDef, List<StatModifier>> GetStatFactorsFromDecorations()
    {
        var dict = new  Dictionary<StatDef, List<StatModifier>>();
        foreach (var statModifier in CurrentAlternateBaseForm.statFactors)
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

        return dict;
    }
    
    public override void PostExposeData()
    {
        Scribe_Defs.Look(ref originalCurrentAlternateBaseForm, "originalCurrentAlternateBaseForm");
        Scribe_Defs.Look(ref currentAlternateBaseForm, "currentAlternateBaseForm");
        
        base.PostExposeData();
        
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }
        
        TryAddCachedStat(Wearer);
        Notify_GraphicChanged();
    }
}