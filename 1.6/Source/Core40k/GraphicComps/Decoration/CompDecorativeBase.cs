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

    //Decorations that have been paid for on this specific item. Lives on the comp instance, so two
    //identical power armours track their unlocks separately and the unlocks travel with the item
    //when it is traded, stored or worn by someone else.
    private List<DecorationDef> unlockedDecorations = [];

    public bool IsUnlocked(DecorationDef decoration)
    {
        return !decoration.HasCost || unlockedDecorations.Contains(decoration);
    }

    public void Unlock(DecorationDef decoration)
    {
        if (!unlockedDecorations.Contains(decoration))
        {
            unlockedDecorations.Add(decoration);
        }
    }

    //Called whenever the decoration dictionary is replaced wholesale rather than through
    //Add/RemoveDecoration. CompWeaponDecoration uses it to rebuild tools and verbs.
    protected virtual void OnDecorationsChanged()
    {
    }

    //Add
    public virtual void ApplyDecorationsFromList(List<DecorationDef> list, bool free = false)
    {
        foreach (var extraDecoration in list)
        {
            AddOrRemoveDecoration(extraDecoration, free);
        }
    }
    public virtual void AddOrRemoveDecoration(DecorationDef decoration, bool free = false)
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
            AddDecoration(decoration, setDefaultColors: true, free: free);
        }
        Notify_GraphicChanged();
    }

    //`free` marks a decoration that came with the item rather than being bought at a station -
    //comp props, pawnkind generation - so it is unlocked outright and never billed.
    protected virtual void AddDecoration(DecorationDef decoration, DecorationSettings decorationSettings = null, bool setDefaultColors = false, bool free = false)
    {
        if (!decorations.ContainsKey(decoration))
        {
            decorations.Add(decoration, decorationSettings ?? new DecorationSettings());
            AddAbilitiesOf(decoration);
        }

        if (!drawDatas.ContainsKey(decoration))
        {
            drawDatas.Add(decoration, new DecorationDrawData());
        }

        if (free)
        {
            Unlock(decoration);
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

        RemoveAbilitiesOf(decoration);
        drawDatas.Remove(decoration);

        return true;
    }
    public virtual void RemoveAllDecorations()
    {
        decorations = new Dictionary<DecorationDef, DecorationSettings>();
        drawDatas = new Dictionary<DecorationDef, DecorationDrawData>();
        Notify_GraphicChanged();
    }

    //Scoped version for the "Remove All" button, which lives on both the Decoration tab and the
    //Upgrades tab and must only clear what the tab it was pressed on is showing.
    //Goes through RemoveDecoration so abilities and weapon tools are cleaned up properly.
    public virtual void RemoveAllDecorations(bool upgrades)
    {
        var toRemove = decorations.Keys.Where(def => def.IsUpgrade == upgrades).ToList();
        foreach (var decoration in toRemove)
        {
            RemoveDecoration(decoration);
        }
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
    //Locked upgrades in a preset are added like anything else and get billed on accept - loading a
    //preset onto a fresh piece is a legitimate way to buy a whole loadout in one go. The pawnkind
    //generation path passes free: true and is never billed.
    public virtual void LoadFromPreset(DecorationPreset preset, bool free = false)
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
            
            AddDecoration(decoDef, extraDecorationsSetting, free: free);
        }
    }
    public void LoadFromPreset(DecorationPresetDef preset, bool free = false)
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
            
            AddDecoration(presetPart.decorationDef, extraDecorationsSetting, free: free);
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
        //Deep copy. AddRange would share the DecorationSettings instances with `decorations`, so a
        //colour or mask change would silently write straight into the "original" state - cancelling
        //could not undo it, and the pending diff could not see it.
        originalDecorations = new Dictionary<DecorationDef, DecorationSettings>();
        foreach (var decoration in decorations)
        {
            originalDecorations.Add(decoration.Key, new DecorationSettings(decoration.Value));
        }
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
        //Deep copy, for the same reason as SetOriginalDecorations.
        drawDatas = new Dictionary<DecorationDef, DecorationDrawData>();
        foreach (var drawData in originalDrawDatas)
        {
            var copy = new DecorationDrawData();
            copy.CopyFrom(drawData.Value);
            drawDatas.Add(drawData.Key, copy);
        }
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
        foreach (var decoration in originalDecorations)
        {
            decorations.Add(decoration.Key, new DecorationSettings(decoration.Value));
        }
    }
    
    //Deferred changes
    private Dictionary<DecorationDef, DecorationSettings> pendingDecorations;
    private Dictionary<DecorationDef, DecorationDrawData> pendingDrawDatas;
    private List<DecorationDef> pendingAdded = [];
    private List<DecorationDef> pendingRemoved = [];
    private List<DecorationDef> pendingUnlock = [];
    private bool pendingAppearance;
    private bool hasPendingChange;

    private void ComputeDiff(out List<DecorationDef> added, out List<DecorationDef> removed, out bool appearanceChanged)
    {
        added = [];
        removed = [];
        appearanceChanged = false;

        foreach (var decoration in decorations)
        {
            if (!originalDecorations.ContainsKey(decoration.Key))
            {
                added.Add(decoration.Key);
                continue;
            }

            var original = originalDecorations[decoration.Key];
            var current = decoration.Value;
            if (original.maskDef != current.maskDef
                || original.Color != current.Color
                || original.ColorTwo != current.ColorTwo
                || original.ColorThree != current.ColorThree
                || original.Flipped != current.Flipped)
            {
                appearanceChanged = true;
            }
        }

        foreach (var decoration in originalDecorations)
        {
            if (!decorations.ContainsKey(decoration.Key))
            {
                removed.Add(decoration.Key);
            }
        }
    }

    public override bool HasEdits
    {
        get
        {
            ComputeDiff(out var added, out var removed, out var appearanceChanged);
            return added.Count > 0 || removed.Count > 0 || appearanceChanged;
        }
    }

    public override bool HasAppearanceEdit
    {
        get
        {
            ComputeDiff(out _, out _, out var appearanceChanged);
            return appearanceChanged;
        }
    }

    public override float EditWork
    {
        get
        {
            ComputeDiff(out var added, out var removed, out _);
            return WorkFor(added, removed);
        }
    }

    public override void CollectEditCost(List<ThingDefCountClass> into)
    {
        if (!DecorationWorkUtility.CostEnabled)
        {
            return;
        }

        ComputeDiff(out var added, out _, out _);
        foreach (var decoration in added)
        {
            if (!IsUnlocked(decoration))
            {
                UpgradeCostUtility.AddCost(into, decoration.cost);
            }
        }
    }

    private static float WorkFor(List<DecorationDef> added, List<DecorationDef> removed)
    {
        var work = 0f;
        foreach (var decoration in added)
        {
            work += decoration.workAmount;
        }
        foreach (var decoration in removed)
        {
            work += decoration.RemovalWork;
        }
        return work;
    }

    public override bool HasPendingChange => hasPendingChange;

    public override bool PendingAppearanceChange => pendingAppearance;

    public override float PendingWork => hasPendingChange ? WorkFor(pendingAdded, pendingRemoved) : 0f;

    public override void CollectPendingCost(List<ThingDefCountClass> into)
    {
        if (!hasPendingChange || !DecorationWorkUtility.CostEnabled)
        {
            return;
        }

        foreach (var decoration in pendingUnlock)
        {
            UpgradeCostUtility.AddCost(into, decoration.cost);
        }
    }

    public override void CapturePending()
    {
        ComputeDiff(out var added, out var removed, out var appearanceChanged);
        if (added.Count == 0 && removed.Count == 0 && !appearanceChanged)
        {
            return;
        }

        pendingDecorations = new Dictionary<DecorationDef, DecorationSettings>();
        foreach (var decoration in decorations)
        {
            pendingDecorations.Add(decoration.Key, new DecorationSettings(decoration.Value));
        }

        pendingDrawDatas = new Dictionary<DecorationDef, DecorationDrawData>();
        foreach (var drawData in drawDatas)
        {
            var copy = new DecorationDrawData();
            copy.CopyFrom(drawData.Value);
            pendingDrawDatas.Add(drawData.Key, copy);
        }

        pendingAdded = added;
        pendingRemoved = removed;
        pendingAppearance = appearanceChanged;
        pendingUnlock = added.Where(def => !IsUnlocked(def)).ToList();
        hasPendingChange = true;

        //Roll the live state back. Abilities were handed out or taken away while the dialog was
        //open, so they have to follow the rollback.
        foreach (var decoration in added)
        {
            RemoveAbilitiesOf(decoration);
        }
        foreach (var decoration in removed)
        {
            AddAbilitiesOf(decoration);
        }

        ResetDecorations();
        ResetDrawDatas();
        OnDecorationsChanged();
        Notify_GraphicChanged();
    }

    public override void CommitPending()
    {
        if (!hasPendingChange)
        {
            return;
        }

        foreach (var decoration in pendingAdded)
        {
            AddAbilitiesOf(decoration);
        }
        foreach (var decoration in pendingRemoved)
        {
            RemoveAbilitiesOf(decoration);
        }

        decorations = pendingDecorations;
        drawDatas = pendingDrawDatas;

        foreach (var decoration in pendingUnlock)
        {
            Unlock(decoration);
        }

        ClearPending();
        SetOriginals();
        OnDecorationsChanged();
        Notify_GraphicChanged();
    }

    public override void DiscardPending()
    {
        //The live state was already rolled back when the change was captured, so there is nothing
        //to undo here.
        ClearPending();
    }

    private void ClearPending()
    {
        pendingDecorations = null;
        pendingDrawDatas = null;
        pendingAdded = [];
        pendingRemoved = [];
        pendingUnlock = [];
        pendingAppearance = false;
        hasPendingChange = false;
    }

    private void AddAbilitiesOf(DecorationDef decoration)
    {
        if (Pawn == null)
        {
            return;
        }
        Pawn.AddAbilities(decoration.givesAbilities, decoration.givesVFEAbilities);
    }

    private void RemoveAbilitiesOf(DecorationDef decoration)
    {
        if (Pawn == null)
        {
            return;
        }
        Pawn.RemoveAbilities(decoration.givesAbilities, decoration.givesVFEAbilities);
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

        Scribe_Collections.Look(ref unlockedDecorations, "unlockedDecorations", LookMode.Def);

        Scribe_Values.Look(ref hasPendingChange, "hasPendingChange");
        Scribe_Values.Look(ref pendingAppearance, "pendingAppearance");
        Scribe_Collections.Look(ref pendingDecorations, "pendingDecorations");
        Scribe_Collections.Look(ref pendingDrawDatas, "pendingDrawDatas");
        Scribe_Collections.Look(ref pendingAdded, "pendingAdded", LookMode.Def);
        Scribe_Collections.Look(ref pendingRemoved, "pendingRemoved", LookMode.Def);
        Scribe_Collections.Look(ref pendingUnlock, "pendingUnlock", LookMode.Def);

        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        decorations ??= new Dictionary<DecorationDef, DecorationSettings>();
        drawDatas ??= new Dictionary<DecorationDef, DecorationDrawData>();
        unlockedDecorations ??= [];
        pendingAdded ??= [];
        pendingRemoved ??= [];
        pendingUnlock ??= [];

        //Save compatibility. Anything already fitted to this item was, by definition, either paid
        //for or came with it, so it is unlocked outright. Without this, an existing save would
        //suddenly show every decoration the colony is already wearing as locked and unpaid.
        //Also correct for new saves, where it is a no-op: whatever is applied is already unlocked.
        foreach (var decoration in decorations)
        {
            if (decoration.Key != null)
            {
                Unlock(decoration.Key);
            }
        }

        //originalDecorations is not scribed, so after a load it is empty until a customization
        //dialog calls SetOriginals. Anything asking for the live-versus-committed diff before that
        //(the accept handler walks every comp on the pawn) would otherwise see every fitted
        //decoration as newly added and bill for it. What is loaded IS the committed state.
        SetOriginalDecorations();
        SetOriginalDrawDatas();

        //A pending change without its snapshot is not recoverable, so drop it rather than
        //committing something half read.
        if (hasPendingChange && (pendingDecorations == null || pendingDrawDatas == null))
        {
            ClearPending();
        }

        TryAddCachedStat(Wearer);
    }
}