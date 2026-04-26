using System.Collections.Generic;
using System.Linq;
using ColourPicker;
using UnityEngine;
using Verse;

namespace Core40k;

public class WeaponDecorationTab : CustomizerTabDrawer
{
    private static Core40kModSettings modSettings = null;
    public static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();

    private float curY;
    
    private ThingWithComps weapon;
    private CompWeaponDecoration WeaponDecorationComp => weapon.GetComp<CompWeaponDecoration>();
    
    private List<WeaponDecorationPresetDef> weaponDecorationPresets = [];
    private Dictionary<WeaponDecorationTypeDef, List<WeaponDecorationDef>> weaponDecorations = new();
    
    private AlternateBaseFormDef selectedAlternateBaseForm = null;
    
    private Vector2 weaponDecorationScrollPosition;
    
    public override void Setup(Pawn pawn)
    {
        weapon = pawn.equipment.Primary;

        if (weapon.HasComp<CompWeaponDecoration>())
        {
            var weaponDecoGrouping = DefDatabase<WeaponDecorationDef>.AllDefs.Where(def => def.appliesTo.Contains(weapon.def.defName) || def.appliesToAll).GroupBy(def => def.decorationType);
            foreach (var grouping in weaponDecoGrouping)
            {
                weaponDecorations.Add(grouping.Key, grouping.ToList());
            }
            
            WeaponDecorationComp.SetOriginals();
            WeaponDecorationComp.RemoveInvalidDecorations(pawn);
        }
        
        foreach (var weaponDecorationPresetDef in DefDatabase<WeaponDecorationPresetDef>.AllDefs)
        {
            weaponDecorationPresets.Add(weaponDecorationPresetDef);
        }
    }

    public override void DrawTab(Rect rect, Pawn pawn, ref Vector2 apparelColorScrollPosition)
    {
        var presetRect = new Rect(rect)
        {
            height = rect.height / 16
        };
        presetRect.width /= 3;
        var removeAllRect = new Rect(presetRect);
        var editPresetRect = new Rect(presetRect);
        
        presetRect.x += presetRect.width*2;
        editPresetRect.x += editPresetRect.width*1;

        rect.yMin += presetRect.height;
        
        removeAllRect = removeAllRect.ContractedBy(3f);
        editPresetRect = editPresetRect.ContractedBy(3f);
        presetRect = presetRect.ContractedBy(3f);

        removeAllRect.x -= 2.5f;
        presetRect.x += 2.5f;
        
        if (Widgets.ButtonText(presetRect, "BEWH.Framework.Customization.DecorationPreset".Translate()))
        {
            var floatMenuOptions = new List<FloatMenuOption>();
        
            var modsettingPresets = ModSettings.ExtraDecorationPresets.Where(deco => deco.appliesTo == weapon.def.defName);
            
            foreach (var preset in modsettingPresets)
            {
                var menuOption = new FloatMenuOption(preset.name, delegate
                {
                    WeaponDecorationComp.RemoveAllDecorations();
                    WeaponDecorationComp.LoadFromPreset(preset);
                });
                floatMenuOptions.Add(menuOption);
            }
            
            foreach (var weaponDecorationPresetDef in weaponDecorationPresets.Where(deco => deco.appliesTo.Contains(weapon.def)))
            {
                var menuOption = new FloatMenuOption(weaponDecorationPresetDef.label, delegate
                {
                    WeaponDecorationComp.RemoveAllDecorations();
                    WeaponDecorationComp.LoadFromPreset(weaponDecorationPresetDef);
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

        if (Widgets.ButtonText(editPresetRect, "BEWH.Framework.Customization.EditPreset".Translate()))
        {
            var floatMenuOptions = new List<FloatMenuOption>();
            
            var currentPreset = GetCurrentPreset("");
            
            var modsettingPresets = ModSettings.ExtraDecorationPresets.Where(deco => deco.appliesTo == weapon.def.defName);
            
            //Delete or override existing
            foreach (var preset in modsettingPresets)
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

        if (Widgets.ButtonText(removeAllRect, "BEWH.Framework.Customization.RemoveAllDecorations".Translate()))
        {
            WeaponDecorationComp.RemoveAllDecorations();
        }
        
        var viewRect = new Rect(rect)
        {
            y = removeAllRect.yMax
        };

        viewRect.width -= 16f;

        var headerHeight = viewRect.height / 12;
        var decoHeight = viewRect.width / 4;
        var iconSize = new Vector2(decoHeight, decoHeight);

        viewRect.height = curY;
        
        var curX = viewRect.x;
        curY = viewRect.y;
        Widgets.BeginScrollView(rect, ref weaponDecorationScrollPosition, viewRect);
        
        var rowExpanded = false;
        
        foreach (var weaponDecoration in weaponDecorations)
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
            Widgets.Label(headerRect, weaponDecoration.Key.label);
            Text.Anchor = TextAnchor.UpperLeft;
            
            for (var i = 0; i < weaponDecoration.Value.Count; i++)
            {
                var hasDeco = WeaponDecorationComp.WeaponDecorations.ContainsKey(weaponDecoration.Value[i]);
                
                var position = new Vector2(curX, curY);
                var iconRect = new Rect(position, iconSize);
                curX += iconRect.width;
                    
                iconRect = iconRect.ContractedBy(5f);
                
                if (hasDeco)
                {
                    Widgets.DrawStrongHighlight(iconRect.ExpandedBy(3f));
                }

                var hasReq = weaponDecoration.Value[i].HasRequirements(pawn, out var reason);
                var incompatibleDeco = (selectedAlternateBaseForm != null 
                                            && selectedAlternateBaseForm.incompatibleWeaponDecorations.Contains(weaponDecoration.Value[i])) 
                                            || (selectedAlternateBaseForm == null
                                            && weaponDecoration.Value[i].isIncompatibleWithBaseTexture);
                
                var color = Color.white;
                var tipTooltip = weaponDecoration.Value[i].TooltipDescription();
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
                GUI.DrawTexture(iconRect, weaponDecoration.Value[i].Icon);
                TooltipHandler.TipRegion(iconRect, tipTooltip);
                
                if(hasReq && !incompatibleDeco)
                {
                    if (Widgets.ButtonInvisible(iconRect))
                    {
                        WeaponDecorationComp.AddOrRemoveDecoration(weaponDecoration.Value[i]);
                    }
                
                    if (weaponDecoration.Value[i].colourable && WeaponDecorationComp.WeaponDecorations.ContainsKey(weaponDecoration.Value[i]))
                    {
                        rowExpanded = true;
                    
                        var bottomRect = new Rect(new Vector2(iconRect.x, iconRect.yMax + 3f), iconRect.size);
                        bottomRect.height /= 3;
                        bottomRect = bottomRect.ContractedBy(2f);
                    
                        Rect colourSelection;
                        Rect colourSelectionTwo;
                        Rect colourSelectionThree;
                
                        var colorAmount = weaponDecoration.Value[i].colorAmount;
                    
                        switch (colorAmount)
                        {
                            case 1:
                                colourSelection = new Rect(bottomRect);
                        
                                PrimaryColorBox(colourSelection, weaponDecoration.Value[i]);
                                break;
                            case 2:
                                colourSelection = new Rect(bottomRect);
                                colourSelection.width /= 2;
                                colourSelectionTwo = new Rect(colourSelection)
                                {
                                    x = colourSelection.xMax
                                };

                                PrimaryColorBox(colourSelection, weaponDecoration.Value[i]);
                                SecondaryColorBox(colourSelectionTwo, weaponDecoration.Value[i]);
                                break;
                            case 3:
                                colourSelection = new Rect(bottomRect);
                                colourSelection.width /= 3;
                                colourSelectionTwo = new Rect(colourSelection)
                                {
                                    x = colourSelection.xMax
                                };
                                colourSelectionThree = new Rect(colourSelectionTwo)
                                {
                                    x = colourSelectionTwo.xMax
                                };

                                PrimaryColorBox(colourSelection, weaponDecoration.Value[i]);
                                SecondaryColorBox(colourSelectionTwo, weaponDecoration.Value[i]);
                                TertiaryColorBox(colourSelectionThree, weaponDecoration.Value[i]);
                                break;
                            default:
                                Log.Warning("Wrong setup in " + weaponDecoration.Value[i] + "colorAmount is more than 3 or less than 1");
                                break;
                        }
                    }
                }
                
                if (i != 0 && (i+1) % 4 == 0 || i == weaponDecoration.Value.Count - 1)
                {
                    curY += iconSize.x;
                    curX = viewRect.x;
                    if (rowExpanded)
                    {
                        curY += iconRect.height/3;
                        rowExpanded = false;
                    }
                }
            }
        }
        Widgets.EndScrollView();
    }
    
    private ExtraDecorationPreset GetCurrentPreset(string presetName)
    {
        var extraDecorationPresetParts = new List<ExtraDecorationPresetParts>();
        
        foreach (var decoration in WeaponDecorationComp.WeaponDecorations)
        {
            var presetPart = new ExtraDecorationPresetParts()
            {
                extraDecorationDefs = decoration.Key.defName,
                colour = decoration.Value.Color,
                colourTwo = decoration.Value.ColorTwo,
                colourThree = decoration.Value.ColorThree,
            };
                
            extraDecorationPresetParts.Add(presetPart);
        }

        var extraDecorationPreset = new ExtraDecorationPreset()
        {
            extraDecorationPresetParts = extraDecorationPresetParts,
            appliesTo = weapon.def.defName,
            name = presetName
        };

        return extraDecorationPreset;
    }
    
    private void PrimaryColorBox(Rect colourSelection, WeaponDecorationDef weaponDecorationDef)
    {
        colourSelection = colourSelection.ContractedBy(2f);
        Widgets.DrawMenuSection(colourSelection);
        colourSelection = colourSelection.ContractedBy(1f);
        Widgets.DrawRectFast(colourSelection, WeaponDecorationComp.WeaponDecorations[weaponDecorationDef].Color);
        TooltipHandler.TipRegion(colourSelection, "BEWH.Framework.Customization.ChooseCustomColour".Translate());
        if (Widgets.ButtonInvisible(colourSelection))
        {
            Find.WindowStack.Add( new Dialog_ColourPicker( WeaponDecorationComp.WeaponDecorations[weaponDecorationDef].Color, ( newColour ) =>
            {
                WeaponDecorationComp.UpdateDecorationColourOne(weaponDecorationDef, newColour);
            } ) );
        }
    }
    
    private void SecondaryColorBox(Rect colourSelectionTwo, WeaponDecorationDef weaponDecorationDef)
    {
        colourSelectionTwo = colourSelectionTwo.ContractedBy(2f);
        Widgets.DrawMenuSection(colourSelectionTwo);
        colourSelectionTwo = colourSelectionTwo.ContractedBy(1f);
        Widgets.DrawRectFast(colourSelectionTwo, WeaponDecorationComp.WeaponDecorations[weaponDecorationDef].ColorTwo);
        TooltipHandler.TipRegion(colourSelectionTwo, "BEWH.Framework.Customization.ChooseCustomColour".Translate());
        if (Widgets.ButtonInvisible(colourSelectionTwo))
        {
            Find.WindowStack.Add( new Dialog_ColourPicker( WeaponDecorationComp.WeaponDecorations[weaponDecorationDef].ColorTwo, ( newColour ) =>
            {
                WeaponDecorationComp.UpdateDecorationColourTwo(weaponDecorationDef, newColour);
            } ) );
        }
    }
    
    private void TertiaryColorBox(Rect colourSelectionThree, WeaponDecorationDef weaponDecorationDef)
    {
        colourSelectionThree = colourSelectionThree.ContractedBy(2f);
        Widgets.DrawMenuSection(colourSelectionThree);
        colourSelectionThree = colourSelectionThree.ContractedBy(1f);
        Widgets.DrawRectFast(colourSelectionThree, WeaponDecorationComp.WeaponDecorations[weaponDecorationDef].ColorThree);
        TooltipHandler.TipRegion(colourSelectionThree, "BEWH.Framework.Customization.ChooseCustomColour".Translate());
        if (Widgets.ButtonInvisible(colourSelectionThree))
        {
            Find.WindowStack.Add( new Dialog_ColourPicker( WeaponDecorationComp.WeaponDecorations[weaponDecorationDef].ColorThree, ( newColour ) =>
            {
                WeaponDecorationComp.UpdateDecorationColourThree(weaponDecorationDef, newColour);
            } ) );
        }
    }
    
    
    public override void OnClose(Pawn pawn, bool closeOnCancel, bool closeOnClickedOutside)
    {
        OnReset(pawn);
    }

    public override void OnAccept(Pawn pawn)
    {
        WeaponDecorationComp.SetOriginals();
    }
    
    public override void OnReset(Pawn pawn)
    {
        WeaponDecorationComp.Reset();
    }
}