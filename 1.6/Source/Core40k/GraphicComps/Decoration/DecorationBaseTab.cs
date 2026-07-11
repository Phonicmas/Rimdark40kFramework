using System;
using System.Collections.Generic;
using System.Linq;
using ColourPicker;
using RimWorld;
using UnityEngine;
using VEF.Utils;
using Verse;

namespace Core40k;

public class DecorationBaseTab : CustomizerTabDrawer
{
    protected static Core40kModSettings modSettings = null;
    public static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();

    protected Dictionary<(DecorationDef, Rot4), string> xStringBuffers = new();
    protected Dictionary<(DecorationDef, Rot4), string> zStringBuffers = new();
    protected Dictionary<(DecorationDef, Rot4), string> drawSizeStringBuffers = new();
    protected Dictionary<(DecorationDef, Rot4), string> layerStringBuffers = new();
    
    protected List<CompDecorativeBase> decorativeComps = new ();
    private Dictionary<CompDecorativeBase, Dictionary<DecorationTypeDef, List<DecorationDef>>> decorationByTypeForComp = new();
    private Dictionary<DecorationDef, List<MaskDef>> masksForDeco = new ();
    private List<DecorationPresetDef> decorationPresets = [];
    
    private AlternateBaseFormDef selectedAlternateBaseForm = null;
    
    private bool recache = true;
    private Dictionary<(DecorationDef, MaskDef), Material> cachedMaterials = new ();
    
    protected float curY;
    private float curX;
    private float headerHeight;
    private float decoHeight;

    private float viewRectHeight;

    private Vector2 iconSize;
    private Vector2 SmallIconSize => new(iconSize.x / 4, iconSize.y / 4);
    
    private float additionalYIncreaseFromColor = 0f;
    private float additionalYIncreaseFromDebug = 0f;
    private float additionalYIncreaseFromMaskAndPreset = 0f;

    private DecorationDef selectedPrecisionDef = null;
    private CompDecorativeBase selectedPrecisionComp = null;
    
    protected Pawn selPawn;

    protected virtual bool OnlyEditDefaultDrawData => false;

    public override void Setup(Pawn pawn)
    {
        selPawn = pawn;
        SetupHook();
        
        var tempPreset = DefDatabase<DecorationPresetDef>.AllDefs.ToList();
        foreach (var decorativeComp in decorativeComps)
        {
            decorationPresets.AddRangeFast(tempPreset.Where(def => def.appliesTo.Contains(decorativeComp.parent.def)));
        }

        var tempMasks = DefDatabase<MaskDef>.AllDefs.Where(def => def.appliesToKind is AppliesToKind.ExtraDecoration or AppliesToKind.All).ToList();
        var tempDefs = DefDatabase<DecorationDef>.AllDefs.Where(def => def is not AlternateBaseFormDef).ToList();
        
        foreach (var decorativeComp in decorativeComps)
        {
            var decoGroupings = tempDefs.Where(def => def.appliesTo.Contains(decorativeComp.parent.def.defName) || def.appliesToAll).GroupBy(def => def.decorationType);
            var tempDictionary = decoGroupings.ToDictionary(decoGrouping => decoGrouping.Key, decoGrouping => decoGrouping.ToList());

            if (!tempDictionary.NullOrEmpty())
            {
                foreach (var value in tempDictionary.Values)
                {
                    value.SortBy(def => def.sortOrder);
                }
                decorationByTypeForComp.Add(decorativeComp, tempDictionary);
            }
            decorativeComp.SetOriginals();
            decorativeComp.RemoveInvalidDecorations(pawn);
        }

        foreach (var decoration in tempDefs)
        {
            var masksForItem = tempMasks.Where(mask => mask.appliesTo.Contains(decoration.defName) || mask.appliesToKind == AppliesToKind.All).ToList();

            if (!masksForItem.Any())
            {
                continue;
            }
            
            masksForItem.SortBy(def => def.sortOrder);
            masksForDeco.Add(decoration, masksForItem);
        }
    }

    protected virtual void SetupHook() { }

    protected virtual void SelectPresetButton(CompDecorativeBase decorativeComp)
    {
        var floatMenuOptions = new List<FloatMenuOption>();
        
        var modsettingPresets = ModSettings.ExtraDecorationPresets.Where(deco => deco.appliesTo == decorativeComp.parent.def.defName);
            
        foreach (var preset in modsettingPresets)
        {
            var menuOption = new FloatMenuOption(preset.name, delegate
            {
                decorativeComp.RemoveAllDecorations();
                decorativeComp.LoadFromPreset(preset);
            });
            floatMenuOptions.Add(menuOption);
        }
            
        foreach (var weaponDecorationPresetDef in decorationPresets.Where(deco => deco.appliesTo.Contains(decorativeComp.parent.def)))
        {
            var menuOption = new FloatMenuOption(weaponDecorationPresetDef.label, delegate
            {
                decorativeComp.RemoveAllDecorations();
                decorativeComp.LoadFromPreset(weaponDecorationPresetDef);
            });
            floatMenuOptions.Add(menuOption);
        }
            
        if (floatMenuOptions.NullOrEmpty())
        {
            var menuOptionNone = new FloatMenuOption("NoneBrackets".Translate(), null);
            floatMenuOptions.Add(menuOptionNone);
        }
        
        Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
    }

    protected virtual void EditPresetButton(CompDecorativeBase decorativeComp)
    {
        var floatMenuOptions = new List<FloatMenuOption>();
            
        var currentPreset = GetCurrentPreset(decorativeComp);
            
        var modsettingPresets = ModSettings.ExtraDecorationPresets.Where(deco => deco.appliesTo == decorativeComp.parent.def.defName);
            
        //Delete or override existing
        foreach (var preset in modsettingPresets)
        {
            var menuOption = new FloatMenuOption(preset.name, delegate
            {
                Find.WindowStack.Add(new Dialog_ConfirmDecorationPresetOverride(preset, currentPreset));
            }, Widgets.PlaceholderIconTex, Color.white)
            {
                extraPartWidth = 30f,
                extraPartOnGUI = rect1 => Core40kUtils.DeletePreset(rect1, preset),
                tooltip = "BEWH.Framework.Customization.OverridePreset".Translate(preset.name)
            };
            floatMenuOptions.Add(menuOption);
        }
            
        //Create new
        var newPreset = new FloatMenuOption("BEWH.Framework.Customization.NewPreset".Translate(), delegate
        {
            Find.WindowStack.Add( new Dialog_EditExtraDecorationPresets(currentPreset));
        }, Widgets.PlaceholderIconTex, Color.white);
        floatMenuOptions.Add(newPreset);
                
        if (!floatMenuOptions.NullOrEmpty())
        {
            Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
        }
    }

    protected virtual string TooltipHook()
    {
        return string.Empty;
    }
    
    public override void DrawTab(Rect rect, Pawn pawn, ref Vector2 scrollPosition)
    {
        if (selectedPrecisionComp != null && selectedPrecisionDef != null)
        {
            DrawPrecisionMode(rect);
            return;
        }
        
        viewRectHeight = curY;

        var viewRect = new Rect(rect)
        {
            height = viewRectHeight
        };
        
        viewRect.width -= 16f;
        
        curY = viewRect.yMin;
        
        Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
        
        foreach (var decorativeComp in decorativeComps)
        {
            var presetRect = new Rect(viewRect)
            {
                height = rect.height / 16
            };
            presetRect.width /= 3;
            var removeAllRect = new Rect(presetRect);
            var editPresetRect = new Rect(presetRect);
        
            presetRect.x += presetRect.width*2;
            editPresetRect.x += editPresetRect.width*1;

            rect.yMin += presetRect.height;

            removeAllRect.y = curY;
            editPresetRect.y = curY;
            presetRect.y = curY;
        
            removeAllRect = removeAllRect.ContractedBy(3f);
            editPresetRect = editPresetRect.ContractedBy(3f);
            presetRect = presetRect.ContractedBy(3f);

            removeAllRect.x -= 2.5f;
            presetRect.x += 2.5f;
            
            var alternateTexture = decorativeComp.parent.TryGetComp<CompAlternateTexture>();
            if (alternateTexture?.CurrentAlternateBaseForm != null)
            {
                foreach (var decoList in decorationByTypeForComp[decorativeComp])
                {
                    decoList.Value.RemoveAll(def => alternateTexture.CurrentAlternateBaseForm.incompatibleDecorations.Contains(def));
                }
            }
            
            //Select Preset
            if (Widgets.ButtonText(presetRect, "BEWH.Framework.Customization.DecorationPreset".Translate()))
            {
                SelectPresetButton(decorativeComp);
            }

            //Edit Preset
            if (Widgets.ButtonText(editPresetRect, "BEWH.Framework.Customization.EditPreset".Translate()))
            {
                EditPresetButton(decorativeComp);
            }

            //Remove all decos
            if (Widgets.ButtonText(removeAllRect, "BEWH.Framework.Customization.RemoveAllDecorations".Translate()))
            {
                decorativeComp.RemoveAllDecorations();
            }

            headerHeight = removeAllRect.height * 1.25f;
            decoHeight = viewRect.width / ModSettings.decorationsPerRow;
            iconSize = new Vector2(decoHeight, decoHeight);

            curY = removeAllRect.yMax;
            curX = viewRect.x;
            
            var headerRect = new Rect(viewRect)
            {
                height = headerHeight * 1.5f,
                y = curY
            };
            curY += headerRect.height;
            headerRect = headerRect.ContractedBy(5f);
            Widgets.DrawMenuSection(headerRect.ContractedBy(-1));
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(headerRect, decorativeComp.parent.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            
            foreach (var decorationByType in decorationByTypeForComp[decorativeComp])
            {
                if (decorationByType.Value.NullOrEmpty())
                {
                    continue;
                }
                DrawDecorationsForType(viewRect, decorativeComp, decorationByType.Key, decorationByType.Value, rect);
            }
        }
        
        Widgets.EndScrollView();
    }

    private void DrawPrecisionMode(Rect rect)
    {
        var drawData = new DecorationDrawData();
        selectedPrecisionComp.drawDatas ??= new Dictionary<DecorationDef, DecorationDrawData>();
        if (!selectedPrecisionComp.drawDatas.TryGetValue(selectedPrecisionDef, out var value))
        {
            selectedPrecisionComp.drawDatas.Add(selectedPrecisionDef, drawData);
        }
        else
        {
            drawData = value;
        }
        
        var precisionRect = new Rect(rect);

        var goBackButton = precisionRect.TakeBottomPart(precisionRect.height/12);

        var acceptButton = goBackButton.TakeRightPart(goBackButton.width / 2);
        
        Widgets.DrawMenuSection(precisionRect);
        var rotsToDraw = new List<Rot4>();

        if (selectedPrecisionComp.parent is Apparel)
        {
            rotsToDraw.Add(Rot4.West);
            rotsToDraw.Add(Rot4.South);
            rotsToDraw.Add(Rot4.East);
            rotsToDraw.Add(Rot4.North);
        }
        else
        {
            rotsToDraw.Add(Rot4.Invalid);
        }
        
        var takeSize = precisionRect.height / rotsToDraw.Count;
        
        foreach (var rotToDraw in rotsToDraw)
        {
            var rotRect = precisionRect.TakeTopPart(takeSize);
            DrawPrecisionOptions(rotRect, ref drawData.GetData(rotToDraw), rotToDraw);
        }
        
        selectedPrecisionComp.SetDrawData(selectedPrecisionDef, drawData);
        
        if (Widgets.ButtonText(acceptButton, "BEWH.Framework.Customization.Accept".Translate()))
        {
            PrecisionReset();
        }
        
        if (Widgets.ButtonText(goBackButton, "BEWH.Framework.Customization.Exit".Translate()))
        {
            selectedPrecisionComp.ResetDrawData(selectedPrecisionDef);
            PrecisionReset();
        }
    }

    private void PrecisionReset()
    {
        selectedPrecisionComp = null;
        selectedPrecisionDef = null;
        xStringBuffers = new Dictionary<(DecorationDef, Rot4), string>();
        zStringBuffers = new Dictionary<(DecorationDef, Rot4), string>();
        drawSizeStringBuffers = new Dictionary<(DecorationDef, Rot4), string>();
        layerStringBuffers = new Dictionary<(DecorationDef, Rot4), string>();
    }

    private void DrawPrecisionOptions(Rect rect, ref DecorationDrawData.RotationalData drawData, Rot4 rot4)
    {
        var defaultRot = rot4 == Rot4.Invalid;
        var takeSize = rect.width / 5;

        var rotLabelRect = rect.TakeLeftPart(takeSize);
        
        var offsetXRect = rect.TakeLeftPart(takeSize).ContractedBy(6);
        var offsetZRect = rect.TakeLeftPart(takeSize).ContractedBy(6);
        var drawSizeRect = rect.TakeLeftPart(takeSize).ContractedBy(6);
        var layerRect = rect.TakeLeftPart(takeSize).ContractedBy(6);

        var resetRect = !defaultRot ? rotLabelRect.TakeBottomPart(rotLabelRect.height / 2) : new Rect(rotLabelRect);

        if (defaultRot)
        {
            resetRect.height /= 8;
            resetRect.y += resetRect.height * 3.5f;
            resetRect = resetRect.ContractedBy(10f);
        }
        else
        {
            resetRect.height /= 2;
            resetRect.width /= 2;
            resetRect.x += resetRect.width/2;
            resetRect = resetRect.ContractedBy(5f);
        }
        
        if (Widgets.ButtonText(resetRect, "BEWH.Framework.CommonKeyword.Reset".Translate()))
        {
            selectedPrecisionComp.ResetDrawData(selectedPrecisionDef, rot4);
            xStringBuffers.Remove((selectedPrecisionDef, rot4));
            zStringBuffers.Remove((selectedPrecisionDef, rot4));
            drawSizeStringBuffers.Remove((selectedPrecisionDef, rot4));
            layerStringBuffers.Remove((selectedPrecisionDef, rot4));
        }
        
        if (!xStringBuffers.ContainsKey((selectedPrecisionDef, rot4)))
        {
            xStringBuffers.Add((selectedPrecisionDef, rot4), drawData.offset.x.ToString());
        }
        if (!zStringBuffers.ContainsKey((selectedPrecisionDef, rot4)))
        {
            zStringBuffers.Add((selectedPrecisionDef, rot4), drawData.offset.y.ToString());
        }
        if (!drawSizeStringBuffers.ContainsKey((selectedPrecisionDef, rot4)))
        {
            drawSizeStringBuffers.Add((selectedPrecisionDef, rot4), drawData.scale.ToString());
        }
        if (!layerStringBuffers.ContainsKey((selectedPrecisionDef, rot4)))
        {
            layerStringBuffers.Add((selectedPrecisionDef, rot4), drawData.layer.ToString());
        }
        
        var xStringBuffer = xStringBuffers[(selectedPrecisionDef, rot4)];
        var zStringBuffer = zStringBuffers[(selectedPrecisionDef, rot4)];
        var drawSizeStringBuffer = drawSizeStringBuffers[(selectedPrecisionDef, rot4)];
        var layerStringBuffer = layerStringBuffers[(selectedPrecisionDef, rot4)];
        
        if (!defaultRot)
        {
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(rotLabelRect, drawData.rotation.ToStringHuman());
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        Core40kUtils.TextFieldWithHorizontalSlider(ref offsetXRect, ref drawData.offset.x, ref xStringBuffer, "X Offset", -2f, 2f);
        Core40kUtils.TextFieldWithHorizontalSlider(ref offsetZRect, ref drawData.offset.z, ref zStringBuffer, "Z Offset", -2f, 2f);
        Core40kUtils.TextFieldWithHorizontalSlider(ref drawSizeRect, ref drawData.scale, ref drawSizeStringBuffer, "Draw Size", 0, 3f);
        Core40kUtils.TextFieldWithHorizontalSlider(ref layerRect, ref drawData.layer, ref layerStringBuffer, "Layer", -100, 100, true);
        
        xStringBuffers[(selectedPrecisionDef, rot4)] = xStringBuffer;
        zStringBuffers[(selectedPrecisionDef, rot4)] = zStringBuffer;
        drawSizeStringBuffers[(selectedPrecisionDef, rot4)] = drawSizeStringBuffer;
        layerStringBuffers[(selectedPrecisionDef, rot4)] = layerStringBuffer;
    }
    
    private DecorationPreset GetCurrentPreset(CompDecorativeBase decorativeComp)
    {
        var extraDecorationPresetParts = new List<DecorationPresetParts>();
        
        foreach (var decoration in decorativeComp.Decorations)
        {
            var presetPart = new DecorationPresetParts()
            {
                extraDecorationDefs = decoration.Key.defName,
                colour = decoration.Value.Color,
                colourTwo = decoration.Value.ColorTwo,
                colourThree = decoration.Value.ColorThree,
                flipped = decoration.Value.Flipped,
                maskDef = decoration.Value.maskDef,
            };
                
            extraDecorationPresetParts.Add(presetPart);
        }

        var extraDecorationPreset = new DecorationPreset()
        {
            decorationPresetParts = extraDecorationPresetParts,
            appliesTo = decorativeComp.parent.def.defName,
            name = string.Empty
        };

        return extraDecorationPreset;
    }

    private void DrawDecorationsForType(Rect viewRect, CompDecorativeBase decorativeComp, DecorationTypeDef decorationTypeDef, List<DecorationDef> decorationDefs, Rect mainRect)
    {
        var headerRect = new Rect(viewRect)
        {
            height = headerHeight,
            y = curY
        };
        curY += headerRect.height;
        headerRect = headerRect.ContractedBy(5f);
        
        Widgets.DrawMenuSection(headerRect.ContractedBy(-1));
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(headerRect, decorationTypeDef.label);
        Text.Anchor = TextAnchor.UpperLeft;
        
        for (var i = 0; i < decorationDefs.Count; i++)
        {
            var decoDef = decorationDefs[i];
            var hasDeco = decorativeComp.Decorations.ContainsKey(decoDef);
            
            var position = new Vector2(curX, curY);
            var iconRect = new Rect(position, iconSize);
                
            iconRect = iconRect.ContractedBy(5f);
            
            if (hasDeco)
            {
                Widgets.DrawStrongHighlight(iconRect.ExpandedBy(3f));
            }

            var hasReq = decoDef.HasRequirements(selPawn, out var reason);
            var incompatibleDeco = DecoIsIncompatible(decoDef, decorativeComp);
            
            var color = Color.white;
            var tipTooltip = decoDef.TooltipDescription();
            if (Mouse.IsOver(iconRect))
            {
                color = GenUI.MouseoverColor;
            }
            if (!hasReq)
            {
                tipTooltip += "\n" + "BEWH.Framework.Customization.RequirementNotMet".Translate() + reason;
                color = Color.gray;
            }
            if (incompatibleDeco)
            {
                tipTooltip += "\n" +"BEWH.Framework.Customization.IncompatibleWithCurrentAltBase".Translate();
                color = Color.gray;
            }
            
            GUI.color = color;
            GUI.DrawTexture(iconRect, Command.BGTexShrunk);
            GUI.color = Color.white;
            GUI.DrawTexture(iconRect, decoDef.Icon);
            TooltipHandler.TipRegion(iconRect, tipTooltip);
            
            if (hasDeco)    
            {
                if (decorativeComp.Decorations[decoDef].Flipped)
                {
                    var flippedIconRect = new Rect(new Vector2(position.x + 7f, position.y + 5f), SmallIconSize);
                    GUI.DrawTexture(flippedIconRect, Core40kUtils.FlippedIconTex);
                }
            }
            
            if(hasReq && !incompatibleDeco)
            {
                if (Widgets.ButtonInvisible(iconRect))
                {
                    decorativeComp.AddOrRemoveDecoration(decoDef);
                }
                if (decorativeComp.Decorations.ContainsKey(decoDef))
                {
                    var bottomRect = new Rect(new Vector2(iconRect.x, iconRect.yMax + 3f), iconRect.size);
                    bottomRect.height /= 3;
                    
                    //Color Selection
                    if (decoDef.colourable)
                    {
                        DrawColorBoxes(ref bottomRect, decoDef, decorativeComp);
                        
                        //Masking and Color Preset
                        DrawColorPresetAndMaskOptions(ref bottomRect, decoDef, decorativeComp);
                    }
                    
                    //Debug offset TODO: Expand for layering or add hook here?
                    if (ModSettings.showCustomizationDebugOptions)
                    {
                        DrawPrecisionButton(ref bottomRect, decoDef, decorativeComp, mainRect);
                    }
                }
            }
            
            curX += iconSize.x;
            
            if (i != 0 && (i+1) % ModSettings.decorationsPerRow == 0 || i == decorationDefs.Count - 1)
            {
                curY += iconSize.x;
                curX = viewRect.x;

                curY += additionalYIncreaseFromDebug + additionalYIncreaseFromColor + additionalYIncreaseFromMaskAndPreset;
                additionalYIncreaseFromDebug = 0;
                additionalYIncreaseFromColor = 0;
                additionalYIncreaseFromMaskAndPreset = 0;
            }
        }
        curY += 34f;
    }

    private bool DecoIsIncompatible(DecorationDef decoDef, CompDecorativeBase decorativeComp)
    {
        //Alternate base form incompatible
        if (selectedAlternateBaseForm != null && selectedAlternateBaseForm.incompatibleDecorations.Contains(decoDef))
        {
            return true;
        }
        //Deco incompatible with base texture
        if (selectedAlternateBaseForm == null && decoDef.isIncompatibleWithBaseTexture)
        {
            return true;
        }
        if (!decoDef.incompatibleDecorations.NullOrEmpty() && decoDef.incompatibleDecorations.Any(def => decorativeComp.Decorations.ContainsKey(def)))
        {
            return true;
        }

        return decorativeComp.Decorations.Any(decoration => decoration.Key.incompatibleDecorations.Contains(decoDef));
    }

    private void DrawColorPresetAndMaskOptions(ref Rect bottomRect, DecorationDef decoDef, CompDecorativeBase decorativeComp)
    {
        var hasMask = masksForDeco.TryGetValue(decoDef).Count > 1;
        var presetSelection = new Rect(bottomRect)
        {
            y = bottomRect.yMax
        };
        if (hasMask)
        {
            presetSelection.width /= 2;
        }
        var maskSelection = new Rect(presetSelection)
        {
            x = presetSelection.xMax
        };
                        
        presetSelection = presetSelection.ContractedBy(1f);
        maskSelection = maskSelection.ContractedBy(1f);
        
        TooltipHandler.TipRegion(presetSelection, "BEWH.Framework.Customization.ColorPresetShortDesc".Translate());
        if (Widgets.ButtonText(presetSelection, "BEWH.Framework.Customization.DecorationPreset".Translate()))
        {
            SelectPreset(decorativeComp, decoDef);
        }
        
        if (hasMask)
        {
            TooltipHandler.TipRegion(maskSelection, "BEWH.Framework.Customization.MaskDesc".Translate());
            if (Widgets.ButtonText(maskSelection, "BEWH.Framework.Customization.Mask".Translate()))
            {
                SelectMask(decorativeComp, decoDef);
            }
        }
        
        additionalYIncreaseFromMaskAndPreset = presetSelection.height;
    }
    
    private void DrawPrecisionButton(ref Rect bottomRect, DecorationDef decoDef, CompDecorativeBase decorativeComp, Rect mainRect)
    {
        var yVal = !decoDef.colourable ? bottomRect.yMin : bottomRect.yMax;
        var precisionRect = new Rect(bottomRect)
        {
            y = yVal + 3f,
        };

        if (decoDef.colourable)
        {
            precisionRect.y += additionalYIncreaseFromMaskAndPreset;
        }

        precisionRect = precisionRect.ContractedBy(1f);

        var totalHeight = precisionRect.height;
        
        if (totalHeight > additionalYIncreaseFromDebug)
        {
            additionalYIncreaseFromDebug = totalHeight;
        }
        
        if (Widgets.ButtonText(precisionRect,"BEWH.Framework.Customization.Precision".Translate()))
        {
            selectedPrecisionDef = decoDef;
            selectedPrecisionComp = decorativeComp;
            selectedPrecisionComp.SetOriginalDrawData(selectedPrecisionDef);
        }
    }
    
    private void DrawColorBoxes(ref Rect bottomRect, DecorationDef decoDef, CompDecorativeBase decorativeComp)
    {
        var colourSelection = new Rect(bottomRect);
        Rect colourSelectionTwo;

        var colorAmount = decoDef.colorAmount;
        
        if (decorativeComp.Decorations[decoDef].maskDef != null && !decorativeComp.Decorations[decoDef].maskDef.setsNull)
        {
            colorAmount = decorativeComp.Decorations[decoDef].maskDef.colorAmount;
        }

        switch (colorAmount)
        {
            case 1:
                DecoColorBox(colourSelection, decorativeComp, decoDef, decorativeComp.Decorations[decoDef].Color, 1);
                break;
            case 2:
                colourSelection.width /= 2;
                colourSelectionTwo = new Rect(colourSelection)
                {
                    x = colourSelection.xMax
                };

                DecoColorBox(colourSelection, decorativeComp, decoDef, decorativeComp.Decorations[decoDef].Color, 1);
                DecoColorBox(colourSelectionTwo, decorativeComp, decoDef, decorativeComp.Decorations[decoDef].ColorTwo, 2);
                break;
            case 3:
                colourSelection.width /= 3;
                colourSelectionTwo = new Rect(colourSelection)
                {
                    x = colourSelection.xMax
                };
                var colourSelectionThree = new Rect(colourSelectionTwo)
                {
                    x = colourSelectionTwo.xMax
                };

                DecoColorBox(colourSelection, decorativeComp, decoDef, decorativeComp.Decorations[decoDef].Color, 1);
                DecoColorBox(colourSelectionTwo, decorativeComp, decoDef, decorativeComp.Decorations[decoDef].ColorTwo, 2);
                DecoColorBox(colourSelectionThree, decorativeComp, decoDef, decorativeComp.Decorations[decoDef].ColorThree, 3);
                break;
            default:
                Log.Warning("Wrong setup in " + decoDef + "colorAmount is more than 3 or less than 1");
                break;
        }

        if (colourSelection.height > additionalYIncreaseFromColor)
        {
            additionalYIncreaseFromColor = colourSelection.height;
        }
    }

    private void DecoColorBox(Rect colorRect, CompDecorativeBase decorativeComp, DecorationDef decorationDef, Color currentColor, int colorNum)
    {
        colorRect = colorRect.ContractedBy(2f);
        Widgets.DrawMenuSection(colorRect);
        colorRect = colorRect.ContractedBy(1f);
        Widgets.DrawRectFast(colorRect, currentColor);
        TooltipHandler.TipRegion(colorRect, "BEWH.Framework.Customization.ChooseCustomColour".Translate());
        if (Widgets.ButtonInvisible(colorRect))
        {
            Find.WindowStack.Add( new Dialog_ColourPicker( currentColor, newColour =>
            {
                recache = true;
                switch (colorNum)
                {
                    case 1:
                        decorativeComp.SetDecorationColourOne(decorationDef, newColour);
                        break;
                    case 2:
                        decorativeComp.SetDecorationColourTwo(decorationDef, newColour);
                        break;
                    case 3:
                        decorativeComp.SetDecorationColourThree(decorationDef, newColour);
                        break;
                }
            } ) );
        }
    }
    
    private void SelectMask(CompDecorativeBase decorativeComp, DecorationDef decoDef)
    {
        var list = new List<FloatMenuOption>();
        foreach (var mask in masksForDeco.TryGetValue(decoDef))
        {
            if (!cachedMaterials.ContainsKey((decoDef, mask)) || recache)
            {
                if (recache)
                {
                    cachedMaterials = new Dictionary<(DecorationDef, MaskDef), Material>();
                }

                var path = decoDef.drawnTextureIconPath;
                var shader = mask?.shaderType?.Shader ?? decoDef.shaderType.Shader;
                Graphic_Multi graphic;
                if (decoDef.colorAmount < 3)
                {
                    graphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(path, decoDef.shaderType.Shader, Vector2.one, decorativeComp.Decorations[decoDef].Color, decorativeComp.Decorations[decoDef].ColorTwo);
                }
                else
                {
                    graphic = MultiColorUtils.GetGraphic<Graphic_Multi>(path, shader, Vector2.one, decorativeComp.Decorations[decoDef].Color, decorativeComp.Decorations[decoDef].ColorTwo, decorativeComp.Decorations[decoDef].ColorThree, null, mask?.maskPath ?? path + "_mask");
                }
                var material = graphic.MatSouth;
                cachedMaterials.Add((decoDef, mask), material);
                recache = false;
            }

            var menuOption = new FloatMenuOptionMask(mask.label, delegate
            {
                decorativeComp.SetDecorationMask(decoDef, mask);
            }, null, Color.white, extraPartRightJustified: true, extraPartWidth: 100f, mouseoverGuiAction: delegate(Rect rect)
            {
                Widgets.DrawMenuSection(rect);
                Graphics.DrawTexture(rect, cachedMaterials[(decoDef, mask)].mainTexture, cachedMaterials[(decoDef, mask)]);
            });
            if (decorativeComp.Decorations[decoDef].maskDef == mask)
            {
                menuOption.Disabled = true;
            }

            list.Add(menuOption);
        }

        if (list.NullOrEmpty())
        {
            var menuOptionNone = new FloatMenuOptionMask("NoneBrackets".Translate(), null);
            list.Add(menuOptionNone);
        }
        
        Find.WindowStack.Add(new FloatMenuMask(list));
    }

    private void SelectPreset(CompDecorativeBase decorativeComp, DecorationDef decoDef)
    {
        var presets = decoDef.availablePresets;
        var list = new List<FloatMenuOption>();
        var colorAmount = decoDef.colorAmount;
        
        var colorComp = decorativeComp.parent.GetComp<CompMultiColor>();
        var armorCol1 = colorComp?.DrawColor ?? decorativeComp.parent.DrawColor;
        var armorCol2 = colorComp?.DrawColorTwo ?? decorativeComp.parent.DrawColor;
        var armorCol3 = colorComp?.DrawColorThree ?? decorativeComp.parent.DrawColor;
        
        if (!decorativeComp.Decorations[decoDef].maskDef.setsNull)
        {
            colorAmount = decorativeComp.Decorations[decoDef].maskDef.colorAmount;
        }
        foreach (var preset in presets)
        {
            var menuOption = new FloatMenuOption(preset.label, delegate
            {
                recache = true;
                decorativeComp.SetDecorationColourOne(decoDef, preset.colour);
                decorativeComp.SetDecorationColourTwo(decoDef, preset.colourTwo ?? Color.white);
                decorativeComp.SetDecorationColourThree(decoDef, preset.colourThree ?? Color.white);
            }, Core40kUtils.ThreeColourPreview(preset.colour, preset.colourTwo, preset.colourThree, colorAmount), Color.white);
            list.Add(menuOption);
        }

        if (decoDef.hasParentColourPaletteOption)
        {
            var menuOptionMatch = new FloatMenuOption("BEWH.Framework.Customization.UseParentColor".Translate(), delegate
            {
                decorativeComp.SetDecorationToParentColors(decoDef);
            }, Core40kUtils.ThreeColourPreview(armorCol1,armorCol2, armorCol3, colorAmount), Color.white);
            list.Add(menuOptionMatch);
        }

        var col1 = decoDef.defaultColour ?? (decoDef.useParentColourAsDefault ? armorCol1 : Color.white);
        var col2 = decoDef.defaultColourTwo ?? (decoDef.useParentColourAsDefault ? armorCol2 : Color.white);
        var col3 = decoDef.defaultColourThree ?? (decoDef.useParentColourAsDefault ? armorCol3 : Color.white);
        
        var menuOptionDefault = new FloatMenuOption("BEWH.Framework.Customization.SetDefaultColor".Translate(), delegate
        {
            decorativeComp.SetDefaultColors(decoDef, false);
        }, Core40kUtils.ThreeColourPreview(col1, col2, col3, colorAmount), Color.white);
        list.Add(menuOptionDefault);
                
        if (list.NullOrEmpty())
        {
            var menuOptionNone = new FloatMenuOption("NoneBrackets".Translate(), null);
            list.Add(menuOptionNone);
        }
        
        Find.WindowStack.Add(new FloatMenu(list));
    }
    
    public override void OnClose(Pawn pawn, bool closeOnCancel, bool closeOnClickedOutside)
    {
        OnReset(pawn);
    }

    public override void OnAccept(Pawn pawn)
    {
        foreach (var decorativeComp in decorativeComps)
        {
            decorativeComp.SetOriginals();
        }
    }
    
    public override void OnReset(Pawn pawn)
    {
        foreach (var decorativeComp in decorativeComps)
        {
            decorativeComp.Reset();
        }
    }
}