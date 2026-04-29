using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class CompDecorative : CompGraphicParent
{
    public void TempSetInitialValues(DecorativeApparelMultiColor multiColor)
    {
        extraDecorations = multiColor.ExtraDecorations;
        originalExtraDecorations = multiColor.ExtraDecorations;
    }

    public CompProperties_Decorative Props => (CompProperties_Decorative)props;
    
    private Dictionary<ExtraDecorationDef, ExtraDecorationSettings> originalExtraDecorations = new ();
    private Dictionary<ExtraDecorationDef, ExtraDecorationSettings> extraDecorations = new ();
    
    public Dictionary<ExtraDecorationDef, ExtraDecorationSettings> ExtraDecorations => extraDecorations;

    public CompMultiColor MultiColor => parent.GetComp<CompMultiColor>();
    
    public override void InitialSetup()
    {
        foreach (var extraDecoration in Props.extraDecorations)
        {
            AddOrRemoveDecoration(extraDecoration);
        }
        base.InitialSetup();
    }
    
    public void AddOrRemoveDecoration(ExtraDecorationDef decoration)
    {
        if (extraDecorations.ContainsKey(decoration) && (extraDecorations[decoration].Flipped || !decoration.flipable))
        {
            extraDecorations.Remove(decoration);
        }
        else if (extraDecorations.TryGetValue(decoration, out var setting))
        {
            setting.Flipped = true;
        }
        else
        {
            extraDecorations.Add(decoration, new ExtraDecorationSettings());
            SetDefaultColors(decoration);
        }
        Notify_GraphicChanged();
    }

    public void SetDefaultColors(ExtraDecorationDef decoration, bool resetMaskDef = true)
    {
        extraDecorations[decoration].Color = decoration.defaultColour ?? (decoration.useArmorColourAsDefault ? MultiColor.DrawColor : Color.white);
        extraDecorations[decoration].ColorTwo = decoration.defaultColourTwo ?? (decoration.useArmorColourAsDefault ? MultiColor.DrawColorTwo : Color.white);
        extraDecorations[decoration].ColorThree = decoration.defaultColourThree ?? (decoration.useArmorColourAsDefault ? MultiColor.DrawColorThree : Color.white);
        if (resetMaskDef)
        {
            extraDecorations[decoration].maskDef = decoration.defaultMask;
        }
        Notify_GraphicChanged();
    }
    
    public void SetArmorColors(ExtraDecorationDef decoration)
    {
        extraDecorations[decoration].Color = MultiColor.DrawColor;
        extraDecorations[decoration].ColorTwo = MultiColor.DrawColorTwo;
        extraDecorations[decoration].ColorThree = MultiColor.DrawColorThree;
        Notify_GraphicChanged();
    }

    public void RemoveAllDecorations()
    {
        extraDecorations = new Dictionary<ExtraDecorationDef, ExtraDecorationSettings>();
        Notify_GraphicChanged();
    }

    public void LoadFromPreset(ExtraDecorationPreset preset)
    {
        foreach (var presetPart in preset.extraDecorationPresetParts)
        {
            var decoDef = Core40kUtils.GetArmorDecoDefFromString(presetPart.extraDecorationDefs);
            var extraDecorationsSetting = new ExtraDecorationSettings()
            {
                Flipped = presetPart.flipped,
                Color = presetPart.colour,
                ColorTwo = presetPart.colourTwo,
                ColorThree = presetPart.colourThree,
                maskDef = presetPart.maskDef ?? Core40kDefOf.BEWH_DefaultMask,
            };
            
            extraDecorations.Add(decoDef, extraDecorationsSetting);
        }
    }
    
    public void LoadFromPreset(ExtraDecorationPresetDef preset)
    {
        foreach (var presetPart in preset.presetData)
        {
            var extraDecorationsSetting = new ExtraDecorationSettings()
            {
                Flipped = presetPart.flipped,
                Color = presetPart.colour ?? (presetPart.extraDecorationDef.useArmorColourAsDefault ? parent.DrawColor : Color.white),
                ColorTwo = presetPart.colourTwo ?? Color.white,
                ColorThree = presetPart.colourThree ?? Color.white,
                maskDef = presetPart.maskDef ?? Core40kDefOf.BEWH_DefaultMask,
            };
            
            extraDecorations.Add(presetPart.extraDecorationDef, extraDecorationsSetting);
        }
    }
        
    public void UpdateDecorationColourOne(ExtraDecorationDef decoration, Color colour)
    {
        extraDecorations[decoration].Color = colour;
        Notify_GraphicChanged();
    }
    
    public void UpdateDecorationColourTwo(ExtraDecorationDef decoration, Color colour)
    {
        extraDecorations[decoration].ColorTwo = colour;
        Notify_GraphicChanged();
    }
    
    public void UpdateDecorationColourThree(ExtraDecorationDef decoration, Color colour)
    {
        extraDecorations[decoration].ColorThree = colour;
        Notify_GraphicChanged();
    }

    public void UpdateDecorationMask(ExtraDecorationDef decoration, MaskDef maskDef)
    {
        extraDecorations[decoration].maskDef = maskDef;
        Notify_GraphicChanged();
    }

    public override void SetOriginals()
    {
        originalExtraDecorations = new Dictionary<ExtraDecorationDef, ExtraDecorationSettings>();
        originalExtraDecorations.AddRange(extraDecorations);
        base.SetOriginals();
    }

    public override void Reset()
    {
        extraDecorations = new Dictionary<ExtraDecorationDef, ExtraDecorationSettings>();
        extraDecorations.AddRange(originalExtraDecorations);
        cachedGraphics = [];
        Notify_GraphicChanged();
        base.Reset();
    }

    public void RemoveInvalidDecorations(Pawn pawn)
    {
        var toRemove = new List<ExtraDecorationDef>();
        foreach (var extraDecoration in extraDecorations)
        {
            if (!extraDecoration.Key.HasRequirements(pawn, out _))
            {
                toRemove.Add(extraDecoration.Key);
            }
        }
        foreach (var extraDecorationDef in toRemove)
        {
            extraDecorations.Remove(extraDecorationDef);
        }
    }

    public void RemoveDecorationsIncompatibleWithAlternate(AlternateBaseFormDef alternateBaseFormDef)
    {
        var toRemove = new List<ExtraDecorationDef>();
        foreach (var extraDecoration in extraDecorations)
        {
            if (alternateBaseFormDef == null && extraDecoration.Key.isIncompatibleWithBaseTexture)
            {
                toRemove.Add(extraDecoration.Key);
            }
            else if (alternateBaseFormDef != null && alternateBaseFormDef.incompatibleArmorDecorations.Contains(extraDecoration.Key))
            {
                toRemove.Add(extraDecoration.Key);
            }
        }
        foreach (var extraDecorationDef in toRemove)
        {
            extraDecorations.Remove(extraDecorationDef);
        }
    }
    
    public override void Notify_Equipped(Pawn pawn)
    {
        RemoveInvalidDecorations(pawn);
        Notify_ColorChanged();
        base.Notify_Equipped(pawn);
    }
    
    private List<Graphic> cachedGraphics = [];
    
    //MESS WITH THIS WHEN YOURE GOING TO FIX OUTFIT STANDS NOT SHOWING DECOS, SHOULD BE ABLE TO DRAW FROM HERE.
    /*public override void Notify_GraphicChanged()
    {
        base.Notify_GraphicChanged();
        foreach (var extraDecorationDef in extraDecorations.Keys)
        {
            Graphic graphic;
            if (extraDecorationDef.colorAmount > 2)
            {
                graphic = MultiColorUtils.GetGraphic<Graphic_Single>(
                    extraDecorationDef.drawnTextureIconPath, 
                    extraDecorationDef.shaderType.Shader ?? Core40kDefOf.BEWH_CutoutThreeColor.Shader, 
                    extraDecorationDef.drawSize, 
                    MultiColor?.DrawColor ?? parent.DrawColor, 
                    MultiColor?.DrawColorTwo ?? parent.DrawColorTwo, 
                    MultiColor?.DrawColorThree ?? parent.DrawColorTwo, 
                    parent.def.graphicData,
                    extraDecorationDef.useMask ? extraDecorationDef.defaultMask.maskPath : null);
            }
            else
            {
                graphic = GraphicDatabase.Get<Graphic_Single>(
                    extraDecorationDef.drawnTextureIconPath, 
                    extraDecorationDef.shaderType.Shader ?? ShaderTypeDefOf.Cutout.Shader, 
                    extraDecorationDef.drawSize, 
                    parent.DrawColor, 
                    parent.DrawColorTwo, 
                    parent.def.graphicData);
            }
            
            cachedGraphics.Add(graphic);
        }
    }
    
    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        foreach (var graphic in cachedGraphics)
        {
            graphic.Draw(drawLoc, flip ? parent.Rotation.Opposite : parent.Rotation, parent);
        }
    }*/

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref extraDecorations, "extraDecorations");
        Scribe_Collections.Look(ref originalExtraDecorations, "originalExtraDecorations");
    }
}