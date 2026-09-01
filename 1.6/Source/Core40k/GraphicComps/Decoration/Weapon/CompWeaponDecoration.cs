using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Core40k;

public class CompWeaponDecoration : CompDecorativeBase
{
    public CompProperties_WeaponDecoration Props => (CompProperties_WeaponDecoration)props;
    public override void InitialSetup()
    {
        ApplyDecorationsFromList(Props.decorations, free: true);
        base.InitialSetup();
    }
    
    public override void Reset()
    {
        cachedGraphics = [];
        base.Reset();
        InvalidateToolsAndVerbs();
    }

    public override void Notify_GraphicChanged()
    {
        RecacheDecorationGraphics();
        base.Notify_GraphicChanged();
    }
    
    public bool recacheGraphics = true;
    private Dictionary<DecorationDef, Graphic> cachedGraphics = [];
    public Dictionary<DecorationDef, Graphic> Graphics => cachedGraphics ??= new Dictionary<DecorationDef, Graphic>();
    public void RecacheDecorationGraphics()
    {
        recacheGraphics = false;
        cachedGraphics = [];
        var sortedGraphics = Decorations.Keys.ToList();
        if (!sortedGraphics.NullOrEmpty())
        {
            sortedGraphics.SortBy(def => GetLayerForDeco(def, parent));
        }
        foreach (var weaponDecoration in sortedGraphics)
        {
            Graphic graphic;
            if (weaponDecoration.colorAmount > 2)
            {
                graphic = MultiColorUtils.GetGraphic<Graphic_Single>(
                    weaponDecoration.drawnTextureIconPath, 
                    Core40kDefOf.BEWH_CutoutThreeColor.Shader, 
                    weaponDecoration.drawSize, 
                    decorations[weaponDecoration].Color, 
                    decorations[weaponDecoration].ColorTwo, 
                    decorations[weaponDecoration].ColorThree, 
                    null,
                    weaponDecoration.defaultMask.maskPath == string.Empty ? weaponDecoration.defaultMask.maskPath  : null);
            }
            else
            {
                graphic = GraphicDatabase.Get<Graphic_Single>(
                    weaponDecoration.drawnTextureIconPath, 
                    weaponDecoration.shaderType.Shader ?? ShaderTypeDefOf.Cutout.Shader, 
                    weaponDecoration.drawSize, 
                    decorations[weaponDecoration].Color, 
                    decorations[weaponDecoration].ColorTwo, 
                    null,
                    weaponDecoration.defaultMask.maskPath == string.Empty ? weaponDecoration.defaultMask.maskPath  : null);
            }
            
            cachedGraphics.Add(weaponDecoration, graphic);
        }
    }

    //Tools and verbs granted by decorations
    private List<Tool> cachedTools;
    private List<VerbProperties> cachedVerbProperties;
    private bool toolsAndVerbsCached;

    //Both of these return null when no decoration touches the weapons tools or verbs, which
    //tells the CompEquippable patch to leave the weapons own lists alone.
    public List<Tool> DecoratedTools
    {
        get
        {
            RecacheToolsAndVerbs();
            return cachedTools;
        }
    }

    public List<VerbProperties> DecoratedVerbProperties
    {
        get
        {
            RecacheToolsAndVerbs();
            return cachedVerbProperties;
        }
    }

    public bool AnyDecorationChangesToolsOrVerbs => Decorations.Keys.OfType<WeaponDecorationDef>().Any(deco => deco.ChangesToolsOrVerbs);

    public void InvalidateToolsAndVerbs()
    {
        toolsAndVerbsCached = false;
        cachedTools = null;
        cachedVerbProperties = null;
        //Forces CompEquippable to build its verbs again the next time anything asks for them.
        parent.GetComp<CompEquippable>()?.verbTracker?.VerbsNeedReinitOnLoad();
    }

    private void RecacheToolsAndVerbs()
    {
        if (toolsAndVerbsCached)
        {
            return;
        }

        toolsAndVerbsCached = true;
        cachedTools = null;
        cachedVerbProperties = null;

        //Ordered by defName so verb load ids stay stable between saves.
        var relevantDecorations = Decorations.Keys
            .OfType<WeaponDecorationDef>()
            .Where(deco => deco.ChangesToolsOrVerbs)
            .OrderBy(deco => deco.defName)
            .ToList();

        if (relevantDecorations.NullOrEmpty())
        {
            return;
        }

        var disabledTools = new List<string>();
        var disabledVerbs = new List<string>();
        var disableAllTools = false;

        foreach (var decoration in relevantDecorations)
        {
            disableAllTools |= decoration.disablesAllWeaponTools;
            if (!decoration.disablesWeaponTools.NullOrEmpty())
            {
                disabledTools.AddRange(decoration.disablesWeaponTools);
            }
            if (!decoration.disablesWeaponVerbs.NullOrEmpty())
            {
                disabledVerbs.AddRange(decoration.disablesWeaponVerbs);
            }
        }

        var tools = new List<Tool>();
        if (!parent.def.tools.NullOrEmpty() && !disableAllTools)
        {
            foreach (var tool in parent.def.tools)
            {
                if (!tool.MatchesAny(disabledTools))
                {
                    tools.Add(tool);
                }
            }
        }

        var verbProperties = new List<VerbProperties>();
        foreach (var verb in parent.def.Verbs)
        {
            if (!verb.MatchesAny(disabledVerbs))
            {
                verbProperties.Add(verb);
            }
        }

        foreach (var decoration in relevantDecorations)
        {
            if (!decoration.tools.NullOrEmpty())
            {
                foreach (var tool in decoration.tools)
                {
                    //Copied per weapon, the def level Tool is shared by every weapon wearing
                    //this decoration and Comp_ForceWeapon writes into the tools it is handed.
                    tools.Add(WeaponDecorationVerbUtility.CopyTool(tool));
                }
            }
            if (!decoration.verbs.NullOrEmpty())
            {
                verbProperties.AddRange(decoration.verbs);
            }
        }

        cachedTools = tools;
        cachedVerbProperties = verbProperties;
    }

    //Decoration changes
    protected override void AddDecoration(DecorationDef decoration, DecorationSettings decorationSettings = null, bool setDefaultColors = false, bool free = false)
    {
        base.AddDecoration(decoration, decorationSettings, setDefaultColors, free);
        InvalidateToolsAndVerbs();
    }

    protected override bool RemoveDecoration(DecorationDef decoration)
    {
        if (!base.RemoveDecoration(decoration))
        {
            return false;
        }
        InvalidateToolsAndVerbs();
        return true;
    }

    public override void RemoveAllDecorations()
    {
        base.RemoveAllDecorations();
        InvalidateToolsAndVerbs();
    }

    protected override void OnDecorationsChanged()
    {
        base.OnDecorationsChanged();
        InvalidateToolsAndVerbs();
    }

    public override void RemoveInvalidDecorations(Pawn pawn)
    {
        var before = Decorations.Count;
        base.RemoveInvalidDecorations(pawn);
        if (Decorations.Count != before)
        {
            InvalidateToolsAndVerbs();
        }
    }

    public override void RemoveDecorationsIncompatibleWithAlternate(AlternateBaseFormDef alternateBaseFormDef)
    {
        var before = Decorations.Count;
        base.RemoveDecorationsIncompatibleWithAlternate(alternateBaseFormDef);
        if (Decorations.Count != before)
        {
            InvalidateToolsAndVerbs();
        }
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (var statDrawEntry in base.SpecialDisplayStats())
        {
            yield return statDrawEntry;
        }

        var addedTools = Decorations.Keys
            .OfType<WeaponDecorationDef>()
            .Where(deco => !deco.tools.NullOrEmpty())
            .OrderBy(deco => deco.defName)
            .ToList();

        if (addedTools.NullOrEmpty())
        {
            yield break;
        }

        var report = new StringBuilder();
        var count = 0;
        foreach (var decoration in addedTools)
        {
            report.AppendLine(decoration.LabelCap + ":");
            foreach (var tool in decoration.tools)
            {
                count++;
                report.AppendLine("  " + WeaponDecorationVerbUtility.ToolSummary(tool));
            }
            report.AppendLine();
        }

        yield return new StatDrawEntry(
            StatCategoryDefOf.Weapon_Melee,
            "BEWH.Framework.Customization.AddedMeleeAttacks".Translate(),
            count.ToString(),
            report.ToString().TrimEndNewlines(),
            5000);
    }

    private float GetLayerForDeco(DecorationDef decoDef, Thing eq)
    {
        if (decoDef is not WeaponDecorationDef weaponDecoDef)
        {
            return 0f;
        }
        var layer = weaponDecoDef.layerPlacement;
        if (eq.ParentHolder is not Pawn_EquipmentTracker equipmentTracker)
        {
            return layer;
        }
        if (weaponDecoDef.weaponSpecificDrawData != null && weaponDecoDef.weaponSpecificDrawData.TryGetValue(eq.def.defName, out var value))
        {
            layer = value.LayerForRot(equipmentTracker.pawn.Rotation, layer);
        }
        if (drawDatas.TryGetValue(weaponDecoDef, out var drawData))
        {
            layer += drawData.defaultData.layer;
        }

        return layer;
    }
    
    public override void PostExposeData()
    {
        //TODO: Remove at later point
        Scribe_Collections.Look(ref originalWeaponDecorations, "originalWeaponDecorations");
        Scribe_Collections.Look(ref weaponDecorations, "weaponDecorations");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && !weaponDecorations.NullOrEmpty() && !originalWeaponDecorations.NullOrEmpty())
        {
            FixDecos();
        }
        base.PostExposeData();

        //CompEquippable may well have rebuilt its verbs before this comp finished loading its
        //decorations, so anything that grants tools or verbs gets a clean rebuild here.
        if (Scribe.mode == LoadSaveMode.PostLoadInit && AnyDecorationChangesToolsOrVerbs)
        {
            InvalidateToolsAndVerbs();
        }
    }
    
    [Obsolete]
    private Dictionary<WeaponDecorationDef, ExtraDecorationSettings> originalWeaponDecorations = new ();
    [Obsolete]
    private Dictionary<WeaponDecorationDef, ExtraDecorationSettings> weaponDecorations = new ();
    [Obsolete]
    private void FixDecos()
    {
        decorations ??= new Dictionary<DecorationDef, DecorationSettings>();
        originalDecorations ??= new Dictionary<DecorationDef, DecorationSettings>();
        foreach (var weapDecos in weaponDecorations)
        {
            decorations.SetOrAdd(weapDecos.Key, weapDecos.Value);
        }
        foreach (var orgWeapDecos in originalWeaponDecorations)
        {
            originalDecorations.SetOrAdd(orgWeapDecos.Key, orgWeapDecos.Value);
        }

        weaponDecorations = new Dictionary<WeaponDecorationDef, ExtraDecorationSettings>();
        originalWeaponDecorations = new Dictionary<WeaponDecorationDef, ExtraDecorationSettings>();
    }
}