using System.Collections.Generic;
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
        if (pawn != null && CoreUtils != null)
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
        if (pawn != null && CoreUtils != null)
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

    //Deferred changes. Switching the base texture is charged the same flat appearance work as a
    //recolour, so it commits together with everything else rather than applying early.
    private AlternateBaseFormDef pendingAlternateBaseForm;
    private bool hasPendingChange;

    public override bool HasEdits => HasAppearanceEdit;

    public override bool HasAppearanceEdit => currentAlternateBaseForm != originalCurrentAlternateBaseForm;

    public override bool HasPendingChange => hasPendingChange;

    public override bool PendingAppearanceChange => hasPendingChange;

    public override void CapturePending()
    {
        if (!HasAppearanceEdit)
        {
            return;
        }

        pendingAlternateBaseForm = currentAlternateBaseForm;
        hasPendingChange = true;

        Reset();
    }

    public override void CommitPending()
    {
        if (!hasPendingChange)
        {
            return;
        }

        currentAlternateBaseForm = pendingAlternateBaseForm;
        pendingAlternateBaseForm = null;
        hasPendingChange = false;

        SetOriginals();
        Notify_GraphicChanged();
    }

    public override void DiscardPending()
    {
        pendingAlternateBaseForm = null;
        hasPendingChange = false;
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
        
        foreach (var pair in GetStatModifiersFromAlternateForm(false))
        {
            yield return StatContributionEntry(Core40kDefOf.BEWH_AlternateTextureOffsets, pair.Key, pair.Value, false);
        }
        
        foreach (var pair in GetStatModifiersFromAlternateForm(true))
        {
            yield return StatContributionEntry(Core40kDefOf.BEWH_AlternateTextureFactors, pair.Key, pair.Value, true);
        }
    }
    //Keyed by stat, each contribution labelled with the alternate form it came from, so the info
    //card report names the source rather than only showing a lump sum.
    private Dictionary<StatDef, List<StatContribution>> GetStatModifiersFromAlternateForm(bool factors)
    {
        var dict = new Dictionary<StatDef, List<StatContribution>>();
        var statModifiers = factors ? CurrentAlternateBaseForm.statFactors : CurrentAlternateBaseForm.statOffsets;
        if (statModifiers.NullOrEmpty())
        {
            return dict;
        }

        foreach (var statModifier in statModifiers)
        {
            var contribution = new StatContribution(CurrentAlternateBaseForm.LabelCap, statModifier, "BEWH.Framework.StatReport.AlternateTexture");
            if (dict.TryGetValue(statModifier.stat, out var contributions))
            {
                contributions.Add(contribution);
            }
            else
            {
                dict.Add(statModifier.stat, [contribution]);
            }
        }

        return dict;
    }
    
    public override void PostExposeData()
    {
        Scribe_Defs.Look(ref originalCurrentAlternateBaseForm, "originalCurrentAlternateBaseForm");
        Scribe_Defs.Look(ref currentAlternateBaseForm, "currentAlternateBaseForm");
        Scribe_Values.Look(ref hasPendingChange, "hasPendingChange");
        Scribe_Defs.Look(ref pendingAlternateBaseForm, "pendingAlternateBaseForm");
        
        base.PostExposeData();
        
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }
        
        //Pawn, not Wearer: Wearer is only set for apparel, so an equipped weapon was never
        //registered after a load and contributed none of its stat offsets until it was re-equipped.
        TryAddCachedStat(Pawn);
        Notify_GraphicChanged();
    }
}