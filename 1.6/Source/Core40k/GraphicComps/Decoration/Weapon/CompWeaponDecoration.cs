using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class CompWeaponDecoration : CompDecorativeBase
{
    public CompProperties_WeaponDecoration Props => (CompProperties_WeaponDecoration)props;
    public override void InitialSetup()
    {
        ApplyDecorationsFromList(Props.decorations);
        base.InitialSetup();
    }
    
    public override void Reset()
    {
        cachedGraphics = [];
        base.Reset();
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