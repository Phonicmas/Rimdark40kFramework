using System;
using System.Collections.Generic;
using System.Linq;
using ColourPicker;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class ExtraDecorationTab : CustomizerTabDrawer
{
    private static Core40kModSettings modSettings = null;
    public static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();
        
    private const int RowAmount = 6;

    private static float listScrollViewHeight = 0f;

    private float curY;
    
    private Dictionary<ExtraDecorationDef, List<MaskDef>> masks = new ();
    
    private Dictionary<(ExtraDecorationDef, MaskDef), Material> cachedMaterials = new ();
    
    private bool recache = true;
        
    private List<ExtraDecorationDef> extraDecorationDefsBody = [];
    private List<ExtraDecorationDef> extraDecorationDefsHelmet = [];
    
    private List<ExtraDecorationPresetDef> extraDecorationPresets = [];

    private Pawn selPawn = null;
    
    public override void Setup(Pawn pawn)
    {
        selPawn = pawn;
        var allExtraDecorations = DefDatabase<ExtraDecorationDef>.AllDefs.ToList();
        
        var masksTemp = DefDatabase<MaskDef>.AllDefs.Where(def => def.appliesToKind is AppliesToKind.ExtraDecoration or AppliesToKind.All).ToList();

        foreach (var extraDecoration in allExtraDecorations)
        {
            if (extraDecoration.isHelmetDecoration)
            {
                extraDecorationDefsHelmet.Add(extraDecoration);
            }
            else
            {
                extraDecorationDefsBody.Add(extraDecoration);
            }
            
            var masksForItem = masksTemp.Where(mask => mask.appliesTo.Contains(extraDecoration.defName) || mask.appliesToKind == AppliesToKind.All).ToList();

            if (masksForItem.Any())
            {
                masksForItem.SortBy(def => def.sortOrder);
                masks.Add(extraDecoration, masksForItem);
            }
        }

        extraDecorationDefsBody.SortBy(def => def.sortOrder);
        extraDecorationDefsHelmet.SortBy(def => def.sortOrder);

        foreach (var extraDecorationPresetDef in DefDatabase<ExtraDecorationPresetDef>.AllDefs)
        {
            extraDecorationPresets.Add(extraDecorationPresetDef);
        }
        
        var apparels = pawn.apparel.WornApparel.Where(a =>
        {
            var temp = a.GetComp<CompDecorative>();
            return temp != null;
        }).ToList();
        foreach (var apparel in apparels)
        {
            apparel.GetComp<CompDecorative>().RemoveInvalidDecorations(pawn);
            apparel.GetComp<CompDecorative>().SetOriginals();
        }
    }
    
    public override void DrawTab(Rect rect, Pawn pawn, ref Vector2 apparelColorScrollPosition)
    {
        GUI.BeginGroup(rect);
        var outRect = new Rect(0f, 0f, rect.width, rect.height);
        var viewRect = new Rect(0f, 0f, rect.width - 16f, listScrollViewHeight);
        Widgets.BeginScrollView(outRect, ref apparelColorScrollPosition, viewRect);
            
        var bodyApparels = pawn.apparel.WornApparel.Where(a =>
        {
            var temp = a.GetComp<CompDecorative>();
            return temp != null && temp.Props.decorativeType == DecorativeType.Body;
        }).ToList();
        var helmetApparels = pawn.apparel.WornApparel.Where(a =>
        {
            var temp = a.GetComp<CompDecorative>();
            return temp != null && temp.Props.decorativeType == DecorativeType.Head;
        }).ToList();
            
        curY = viewRect.y;
            
        //Body Decorations
        if (!bodyApparels.NullOrEmpty())
        {
            foreach (var bodyApparel in bodyApparels)
            {
                //Extra decoration title
                var nameRect = new Rect(viewRect.x, curY, viewRect.width, 30f);
                nameRect.width /= 2;
                nameRect.x += nameRect.width / 2;
                Widgets.DrawMenuSection(nameRect);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(nameRect, bodyApparel.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                    
                //Remove All
                var resetAllDecorations = new Rect(nameRect.xMin, curY, viewRect.width, 30f);
                resetAllDecorations.width /= 5;
                resetAllDecorations.x -= resetAllDecorations.width + nameRect.width/20;
                if (Widgets.ButtonText(resetAllDecorations, "BEWH.Framework.Customization.RemoveAllDecorations".Translate()))
                {
                    bodyApparel.GetComp<CompDecorative>().RemoveAllDecorations();
                }
                    
                //Preset Tab
                var presetSelectRect = new Rect(nameRect.xMax, curY, viewRect.width, 30f);
                presetSelectRect.x += nameRect.width/20;
                presetSelectRect.width /= 10;
                presetSelectRect.width -= 2;

                var presetEditRect = new Rect(presetSelectRect);
                presetEditRect.x += presetSelectRect.width + 4;
                    
                //Select Preset
                TooltipHandler.TipRegion(presetSelectRect, "BEWH.Framework.Customization.DecorationPresetDesc".Translate());
                if (Widgets.ButtonText(presetSelectRect, "BEWH.Framework.Customization.DecorationPreset".Translate()))
                {
                    SelectDecorationPreset(bodyApparel);
                }
                    
                //Edit Presets
                TooltipHandler.TipRegion(presetEditRect, "BEWH.Framework.Customization.EditDesc".Translate());
                if (Widgets.ButtonText(presetEditRect, "BEWH.Framework.Customization.Edit".Translate()))
                {
                    EditDecorationPreset(bodyApparel);
                }
                    
                var position = new Vector2(viewRect.x, resetAllDecorations.yMax);
                curY = position.y;    
                
                DrawRowContent(bodyApparel, extraDecorationDefsBody, ref position, ref viewRect);
                
                listScrollViewHeight = curY + 10f;
            }
        }
            
        //Head Decorations
        if (!helmetApparels.NullOrEmpty())
        {
            foreach (var helmetApparel in helmetApparels)
            {
                //Extra decoration title
                var nameRect = new Rect(viewRect.x, curY, viewRect.width, 30f);
                nameRect.width /= 2;
                nameRect.x += nameRect.width / 2;
                Widgets.DrawMenuSection(nameRect);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(nameRect, helmetApparel.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                    
                //Remove All
                var resetAllDecorations = new Rect(nameRect.xMin, curY, viewRect.width, 30f);
                resetAllDecorations.width /= 5;
                resetAllDecorations.x -= resetAllDecorations.width + nameRect.width/20;
                if (Widgets.ButtonText(resetAllDecorations, "BEWH.Framework.Customization.RemoveAllDecorations".Translate()))
                {
                    helmetApparel.GetComp<CompDecorative>().RemoveAllDecorations();
                }
                    
                //Preset Tab
                var presetSelectRect = new Rect(nameRect.xMax, curY, viewRect.width, 30f);
                presetSelectRect.x += nameRect.width/20;
                presetSelectRect.width /= 10;
                presetSelectRect.width -= 2;

                var presetEditRect = new Rect(presetSelectRect);
                presetEditRect.x += presetSelectRect.width + 4;
                    
                //Select Preset
                TooltipHandler.TipRegion(presetSelectRect, "BEWH.Framework.Customization.DecorationPresetDesc".Translate());
                if (Widgets.ButtonText(presetSelectRect, "BEWH.Framework.Customization.DecorationPreset".Translate()))
                {
                    SelectDecorationPreset(helmetApparel);
                }
                    
                //Edit Presets
                TooltipHandler.TipRegion(presetEditRect, "BEWH.Framework.Customization.EditDesc".Translate());
                if (Widgets.ButtonText(presetEditRect, "BEWH.Framework.Customization.Edit".Translate()))
                {
                    EditDecorationPreset(helmetApparel);
                }
                    
                var position = new Vector2(viewRect.x, curY + resetAllDecorations.height);
                curY = position.y;   
                
                DrawRowContent(helmetApparel, extraDecorationDefsHelmet, ref position, ref viewRect);
                
                listScrollViewHeight = curY + 10f;
            }
        }
            
        Widgets.EndScrollView();
        GUI.EndGroup();
    }
    
    private void DrawRowContent(Apparel apparel, List<ExtraDecorationDef> extraDecorationDefs, ref Vector2 position, ref Rect viewRect)
    {
        var iconSize = new Vector2(viewRect.width/RowAmount, viewRect.width/RowAmount);
        var smallIconSize = new Vector2(iconSize.x / 4, iconSize.y / 4);

        var decorativeComp = apparel.GetComp<CompDecorative>();
        
        var currentDecorations = decorativeComp.ExtraDecorations;

        var colourButtonExtraSize = 0f;
            
        var curX = position.x;

        var rowExpanded = false;

        extraDecorationDefs = extraDecorationDefs
            .Where(deco => deco.appliesToAll || deco.appliesTo.Contains(apparel.def.defName))
            .ToList();

        var tempExtraDecorationDefs = new List<ExtraDecorationDef>();
        extraDecorationDefs.CopyToList(tempExtraDecorationDefs);
        
        var multiColorComp = apparel.TryGetComp<CompMultiColor>();
        if (multiColorComp?.CurrentAlternateBaseForm != null)
        {
            foreach (var incompatibleArmorDecoration in multiColorComp.CurrentAlternateBaseForm.incompatibleArmorDecorations)
            {
                if (extraDecorationDefs.Contains(incompatibleArmorDecoration))
                {
                    tempExtraDecorationDefs.Remove(incompatibleArmorDecoration);
                }
            }
        }

        extraDecorationDefs = tempExtraDecorationDefs;
            
        for (var i = 0; i < extraDecorationDefs.Count; i++)
        {
            position = new Vector2(curX, curY);
            var iconRect = new Rect(position, iconSize);
                    
            curX += iconRect.width;
                    
            iconRect = iconRect.ContractedBy(5f);
            
            var hasDeco = currentDecorations.ContainsKey(extraDecorationDefs[i]);
                    
            if (hasDeco)
            {
                Widgets.DrawStrongHighlight(iconRect.ExpandedBy(3f));
            }
            
            var hasReq = extraDecorationDefs[i].HasRequirements(selPawn, out var reason);
            var incompatibleDeco = (multiColorComp?.CurrentAlternateBaseForm != null 
                                        && multiColorComp.CurrentAlternateBaseForm.incompatibleArmorDecorations.Contains(extraDecorationDefs[i]))
                                        || (multiColorComp?.CurrentAlternateBaseForm == null 
                                        && extraDecorationDefs[i].isIncompatibleWithBaseTexture);
                
            var color = Color.white;
            var tipTooltip = extraDecorationDefs[i].TooltipDescription();
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
            GUI.DrawTexture(iconRect, extraDecorationDefs[i].Icon);
                    
            if (hasDeco)    
            {
                if (currentDecorations[extraDecorationDefs[i]].Flipped)
                {
                    var flippedIconRect = new Rect(new Vector2(position.x + 7f, position.y + 5f), smallIconSize);
                    GUI.DrawTexture(flippedIconRect, Core40kUtils.FlippedIconTex);
                }
            }

            TooltipHandler.TipRegion(iconRect, tipTooltip);
            if (hasReq && !incompatibleDeco)
            {
                TooltipHandler.TipRegion(iconRect, extraDecorationDefs[i].TooltipDescription());
            
                if (Widgets.ButtonInvisible(iconRect))
                {
                    decorativeComp.AddOrRemoveDecoration(extraDecorationDefs[i]);
                }
                
                if (extraDecorationDefs[i].colourable && currentDecorations.ContainsKey(extraDecorationDefs[i]))
                {
                    rowExpanded = true;
                    var i1 = i;
                
                    var bottomRect = new Rect(new Vector2(iconRect.x, iconRect.yMax + 3f), iconRect.size);
                    bottomRect.height /= 3;
                    bottomRect = bottomRect.ContractedBy(2f);
                
                    colourButtonExtraSize = bottomRect.height*2;

                    Rect colourSelection;
                    Rect colourSelectionTwo;

                    var colorAmount = extraDecorationDefs[i].colorAmount;
                
                    if (!decorativeComp.ExtraDecorations[extraDecorationDefs[i]].maskDef.setsNull)
                    {
                        colorAmount = decorativeComp.ExtraDecorations[extraDecorationDefs[i]].maskDef.colorAmount;
                    }
                
                    switch (colorAmount)
                    {
                        case 1:
                            colourSelection = new Rect(bottomRect);
                        
                            PrimaryColorBox(colourSelection, decorativeComp, extraDecorationDefs[i1]);
                            break;
                        case 2:
                            colourSelection = new Rect(bottomRect);
                            colourSelection.width /= 2;
                            colourSelectionTwo = new Rect(colourSelection)
                            {
                                x = colourSelection.xMax
                            };

                            PrimaryColorBox(colourSelection, decorativeComp, extraDecorationDefs[i1]);
                            SecondaryColorBox(colourSelectionTwo, decorativeComp, extraDecorationDefs[i1]);
                            break;
                        case 3:
                            colourSelection = new Rect(bottomRect);
                            colourSelection.width /= 3;
                            colourSelectionTwo = new Rect(colourSelection)
                            {
                                x = colourSelection.xMax
                            };
                            var colourSelectionThree = new Rect(colourSelectionTwo)
                            {
                                x = colourSelectionTwo.xMax
                            };

                            PrimaryColorBox(colourSelection, decorativeComp, extraDecorationDefs[i1]);
                            SecondaryColorBox(colourSelectionTwo, decorativeComp, extraDecorationDefs[i1]);
                            TertiaryColorBox(colourSelectionThree, decorativeComp, extraDecorationDefs[i1]);
                            break;
                        default:
                            Log.Warning("Wrong setup in " + extraDecorationDefs[i] + "colorAmount is more than 3 or less than 1");
                            break;
                    }
                
                    var presetSelection = new Rect(bottomRect)
                    {
                        y = bottomRect.yMax
                    };
                    if (masks.TryGetValue(extraDecorationDefs[i]).Count > 1)
                    {
                        presetSelection.width /= 2;
                    }
                    var maskSelection = new Rect(presetSelection)
                    {
                        x = presetSelection.xMax
                    };
                    presetSelection = presetSelection.ContractedBy(1f);
                    maskSelection = maskSelection.ContractedBy(1f);
                    Text.Font = GameFont.Tiny;
                    TooltipHandler.TipRegion(presetSelection, "BEWH.Framework.Customization.ColorPresetShortDesc".Translate());
                    if (Widgets.ButtonText(presetSelection, "BEWH.Framework.Customization.DecorationPreset".Translate()))
                    {
                        SelectPreset(apparel, extraDecorationDefs[i1]);
                    }
                    if (masks.TryGetValue(extraDecorationDefs[i]).Count > 1)
                    {
                        TooltipHandler.TipRegion(maskSelection, "BEWH.Framework.Customization.MaskDesc".Translate());
                        if (Widgets.ButtonText(maskSelection, "BEWH.Framework.Customization.Mask".Translate()))
                        {
                            SelectMask(decorativeComp, extraDecorationDefs[i1], viewRect.width/1.3f);
                        }
                    }
                    Text.Font = GameFont.Small;
                }
            }
            GUI.color = Color.white;
            
            if (i != 0 && (i+1) % RowAmount == 0)
            {
                curY += iconRect.height + 5f;
                if (rowExpanded)
                {
                    rowExpanded = false;
                    curY += colourButtonExtraSize + 5f;
                }
                
                curX = viewRect.position.x;
            }
            else if (i == extraDecorationDefs.Count - 1)
            {
                curY += iconRect.height + 5f;
                if (rowExpanded)
                {
                    rowExpanded = false;
                    curY += colourButtonExtraSize + 5f;
                }
            }
        }

        curY += 34f;
    }

    private void PrimaryColorBox(Rect colourSelection, CompDecorative compDecorative, ExtraDecorationDef extraDecorationDef)
    {
        colourSelection = colourSelection.ContractedBy(2f);
        Widgets.DrawMenuSection(colourSelection);
        colourSelection = colourSelection.ContractedBy(1f);
        Widgets.DrawRectFast(colourSelection, compDecorative.ExtraDecorations[extraDecorationDef].Color);
        TooltipHandler.TipRegion(colourSelection, "BEWH.Framework.Customization.ChooseCustomColour".Translate());
        if (Widgets.ButtonInvisible(colourSelection))
        {
            Find.WindowStack.Add( new Dialog_ColourPicker( compDecorative.ExtraDecorations[extraDecorationDef].Color, ( newColour ) =>
            {
                recache = true;
                compDecorative.UpdateDecorationColourOne(extraDecorationDef, newColour);
            } ) );
        }
    }
    
    private void SecondaryColorBox(Rect colourSelectionTwo, CompDecorative compDecorative, ExtraDecorationDef extraDecorationDef)
    {
        colourSelectionTwo = colourSelectionTwo.ContractedBy(2f);
        Widgets.DrawMenuSection(colourSelectionTwo);
        colourSelectionTwo = colourSelectionTwo.ContractedBy(1f);
        Widgets.DrawRectFast(colourSelectionTwo, compDecorative.ExtraDecorations[extraDecorationDef].ColorTwo);
        TooltipHandler.TipRegion(colourSelectionTwo, "BEWH.Framework.Customization.ChooseCustomColour".Translate());
        if (Widgets.ButtonInvisible(colourSelectionTwo))
        {
            Find.WindowStack.Add( new Dialog_ColourPicker( compDecorative.ExtraDecorations[extraDecorationDef].ColorTwo, ( newColour ) =>
            {
                recache = true;
                compDecorative.UpdateDecorationColourTwo(extraDecorationDef, newColour);
            } ) );
        }
    }
    
    private void TertiaryColorBox(Rect colourSelectionThree, CompDecorative compDecorative, ExtraDecorationDef extraDecorationDef)
    {
        colourSelectionThree = colourSelectionThree.ContractedBy(2f);
        Widgets.DrawMenuSection(colourSelectionThree);
        colourSelectionThree = colourSelectionThree.ContractedBy(1f);
        Widgets.DrawRectFast(colourSelectionThree, compDecorative.ExtraDecorations[extraDecorationDef].ColorThree);
        TooltipHandler.TipRegion(colourSelectionThree, "BEWH.Framework.Customization.ChooseCustomColour".Translate());
        if (Widgets.ButtonInvisible(colourSelectionThree))
        {
            Find.WindowStack.Add( new Dialog_ColourPicker( compDecorative.ExtraDecorations[extraDecorationDef].ColorThree, ( newColour ) =>
            {
                recache = true;
                compDecorative.UpdateDecorationColourThree(extraDecorationDef, newColour);
            } ) );
        }
    }

    private void SelectMask(CompDecorative decorativeComp, ExtraDecorationDef extraDecoration, float size)
    {
        var list = new List<FloatMenuOption>();
        var mouseOffset = UI.MousePositionOnUI;
        foreach (var mask in masks.TryGetValue(extraDecoration))
        {
            if (!cachedMaterials.ContainsKey((extraDecoration, mask)) || recache)
            {
                if (recache)
                {
                    cachedMaterials = new Dictionary<(ExtraDecorationDef, MaskDef), Material>();
                }

                var path = extraDecoration.drawnTextureIconPath;
                var shader = mask?.shaderType?.Shader ?? extraDecoration.shaderType.Shader;
                Graphic_Multi graphic;
                if (!extraDecoration.useMask)
                {
                    graphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(path, extraDecoration.shaderType.Shader, Vector2.one, decorativeComp.ExtraDecorations[extraDecoration].Color);
                }
                else
                {
                    graphic = MultiColorUtils.GetGraphic<Graphic_Multi>(path, shader, Vector2.one, decorativeComp.ExtraDecorations[extraDecoration].Color, decorativeComp.ExtraDecorations[extraDecoration].ColorTwo, decorativeComp.ExtraDecorations[extraDecoration].ColorThree, null, mask?.maskPath ?? path + "_mask");
                }
                var material = graphic.MatSouth;
                cachedMaterials.Add((extraDecoration, mask), material);
                recache = false;
            }
            
            var menuOption = new FloatMenuOption(mask.label, delegate
            {
                decorativeComp.UpdateDecorationMask(extraDecoration, mask);
            }, (Texture2D)null, Color.white, mouseoverGuiAction: delegate(Rect rect)
            {
                if (!Mouse.IsOver(rect))
                {
                    return;
                }
                var pictureSize = new Vector2(100, 100);
                var mouseAttachedWindowPos = new Vector2(rect.width, rect.height);
                mouseAttachedWindowPos.x += mouseOffset.x;
                mouseAttachedWindowPos.y += mouseOffset.y;
                    
                var pictureRect = new Rect(mouseAttachedWindowPos, pictureSize);
                    
                Find.WindowStack.ImmediateWindow(1859615242, pictureRect, WindowLayer.Super, delegate
                {
                    Widgets.DrawMenuSection(pictureRect.AtZero());
                    Graphics.DrawTexture(pictureRect.AtZero(), cachedMaterials[(extraDecoration, mask)].mainTexture, cachedMaterials[(extraDecoration, mask)]);

                });
            });
            if (decorativeComp.ExtraDecorations[extraDecoration].maskDef == mask)
            {
                menuOption.Disabled = true;
            }

            list.Add(menuOption);
        }

        if (list.NullOrEmpty())
        {
            var menuOptionNone = new FloatMenuOption("NoneBrackets".Translate(), null);
            list.Add(menuOptionNone);
        }
        
        Find.WindowStack.Add(new FloatMenu(list));
    }

    private void SelectPreset(Apparel apparel, ExtraDecorationDef extraDecoration)
    {
        var presets = extraDecoration.availablePresets;
        var list = new List<FloatMenuOption>();
        var colorAmount = extraDecoration.colorAmount;

        var decorativeComp = apparel.GetComp<CompDecorative>();
        var colorComp = apparel.GetComp<CompMultiColor>();
        var armorCol1 = colorComp?.DrawColor ?? apparel.DrawColor;
        var armorCol2 = colorComp?.DrawColorTwo ?? apparel.DrawColor;
        var armorCol3 = colorComp?.DrawColorThree ?? apparel.DrawColor;
        
        if (!decorativeComp.ExtraDecorations[extraDecoration].maskDef.setsNull)
        {
            colorAmount = decorativeComp.ExtraDecorations[extraDecoration].maskDef.colorAmount;
        }
        foreach (var preset in presets)
        {
            var menuOption = new FloatMenuOption(preset.label, delegate
            {
                recache = true;
                decorativeComp.UpdateDecorationColourOne(extraDecoration, preset.colour);
                decorativeComp.UpdateDecorationColourTwo(extraDecoration, preset.colourTwo ?? Color.white);
                decorativeComp.UpdateDecorationColourThree(extraDecoration, preset.colourThree ?? Color.white);
            }, Core40kUtils.ThreeColourPreview(preset.colour, preset.colourTwo, preset.colourThree, colorAmount), Color.white);
            list.Add(menuOption);
        }

        if (extraDecoration.hasArmorColourPaletteOption)
        {
            var menuOptionMatch = new FloatMenuOption("BEWH.Framework.Customization.UseArmourColour".Translate(), delegate
            {
                decorativeComp.SetArmorColors(extraDecoration);
            }, Core40kUtils.ThreeColourPreview(armorCol1,armorCol2, armorCol3, colorAmount), Color.white);
            list.Add(menuOptionMatch);
        }

        var col1 = extraDecoration.defaultColour ?? (extraDecoration.useArmorColourAsDefault ? armorCol1 : Color.white);
        var col2 = extraDecoration.defaultColourTwo ?? (extraDecoration.useArmorColourAsDefault ? armorCol2 : Color.white);
        var col3 = extraDecoration.defaultColourThree ?? (extraDecoration.useArmorColourAsDefault ? armorCol3 : Color.white);
        
        var menuOptionDefault = new FloatMenuOption("BEWH.Framework.Customization.SetDefaultColor".Translate(), delegate
        {
            decorativeComp.SetDefaultColors(extraDecoration, false);
        }, Core40kUtils.ThreeColourPreview(col1, col2, col3, colorAmount), Color.white);
        list.Add(menuOptionDefault);
                
        if (list.NullOrEmpty())
        {
            var menuOptionNone = new FloatMenuOption("NoneBrackets".Translate(), null);
            list.Add(menuOptionNone);
        }
        
        Find.WindowStack.Add(new FloatMenu(list));
    }

    private ExtraDecorationPreset GetCurrentPreset(Apparel apparel, string presetName)
    {
        var extraDecorationPresetParts = new List<ExtraDecorationPresetParts>();

        var decorativeComp = apparel.GetComp<CompDecorative>();
        
        foreach (var decoration in decorativeComp.ExtraDecorations)
        {
            var presetPart = new ExtraDecorationPresetParts()
            {
                extraDecorationDefs = decoration.Key.defName,
                flipped = decoration.Value.Flipped,
                colour = decoration.Value.Color,
                colourTwo = decoration.Value.ColorTwo,
                colourThree = decoration.Value.ColorThree,
                maskDef = decoration.Value.maskDef,
            };
                
            extraDecorationPresetParts.Add(presetPart);
        }

        var extraDecorationPreset = new ExtraDecorationPreset()
        {
            extraDecorationPresetParts = extraDecorationPresetParts,
            appliesTo = apparel.def.defName,
            name = presetName
        };

        return extraDecorationPreset;
    }
        
    private void EditDecorationPreset(Apparel apparel)
    {
        var floatMenuOptions = new List<FloatMenuOption>();
            
        var currentPreset = GetCurrentPreset(apparel, "");
            
        var presets = ModSettings.ExtraDecorationPresets.Where(deco => deco.appliesTo == apparel.def.defName);
            
        //Delete or override existing
        foreach (var preset in presets)
        {
            var menuOption = new FloatMenuOption(preset.name, delegate
            {
                Find.WindowStack.Add(new Dialog_ConfirmDecorationPresetOverride(preset, currentPreset));
            }, Widgets.PlaceholderIconTex, Color.white);
            menuOption.extraPartWidth = 30f;
            menuOption.extraPartOnGUI = rect1 => Core40kUtils.DeletePreset(rect1, preset);
            menuOption.tooltip = "BEWH.Framework.Customization.OverridePreset".Translate(preset.name);
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

    private void SelectDecorationPreset(Apparel apparel)
    {
        var presets = ModSettings.ExtraDecorationPresets.Where(deco => deco.appliesTo == apparel.def.defName);
        var list = new List<FloatMenuOption>();
        
        var decorativeComp = apparel.GetComp<CompDecorative>();
            
        foreach (var preset in presets)
        {
            var menuOption = new FloatMenuOption(preset.name, delegate
            {
                decorativeComp.RemoveAllDecorations();
                decorativeComp.LoadFromPreset(preset);
            });
            list.Add(menuOption);
        }

        foreach (var extraDecorationPreset in extraDecorationPresets.Where(deco => deco.appliesTo.Contains(apparel.def)))
        {
            var menuOption = new FloatMenuOption(extraDecorationPreset.label, delegate
            {
                decorativeComp.RemoveAllDecorations();
                decorativeComp.LoadFromPreset(extraDecorationPreset);
            });
            list.Add(menuOption);
        }
                
        if (list.NullOrEmpty())
        {
            var menuOptionNone = new FloatMenuOption("NoneBrackets".Translate(), null);
            list.Add(menuOptionNone);
        }
        
        Find.WindowStack.Add(new FloatMenu(list));
    }
        
    
    public override void OnClose(Pawn pawn, bool closeOnCancel, bool closeOnClickedOutside)
    {
            
    }

    public override void OnAccept(Pawn pawn)
    {
        var apparels = pawn.apparel.WornApparel.Where(a => a.HasComp<CompDecorative>()).ToList();
        foreach (var apparel in apparels)
        {
            apparel.GetComp<CompDecorative>().SetOriginals();
        }
    }
    
    public override void OnReset(Pawn pawn)
    {
        var apparels = pawn.apparel.WornApparel.Where(a => a.HasComp<CompDecorative>()).ToList();
        foreach (var apparel in apparels)
        {
            apparel.GetComp<CompDecorative>().Reset();
        }
    }
}