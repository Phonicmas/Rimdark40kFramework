using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using VEF.Utils;
using Verse;
using AbilityDef = RimWorld.AbilityDef;

namespace Core40k;

[StaticConstructorOnStartup]
public static class Core40kUtils
{
    public static readonly Texture2D FlippedIconTex = ContentFinder<Texture2D>.Get("UI/Decoration/flipIcon");
    public static readonly Texture2D ScrollForwardIcon = ContentFinder<Texture2D>.Get ("UI/Misc/ScrollForwardIcon");
    public static readonly Texture2D ScrollBackwardIcon = ContentFinder<Texture2D>.Get ("UI/Misc/ScrollBackwardIcon");
    
    public static readonly Graphic_Multi EmptyMultiGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>("UI/EmptyImage");
    
    private static Core40kModSettings modSettings = null;
    public static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();

    public static readonly Color RequirementMetColour = Color.white;
    public static readonly Color RequirementNotMetColour = new(1f, 0.0f, 0.0f, 0.8f);

    public static readonly Color LockedColour = new(1f, 0.85f, 0.4f, 0.9f);
    
    public static DecorationDef GetDecoDefFromString(string defName)
    {
        return DefDatabase<DecorationDef>.GetNamedSilentFail(defName);
    }
        
    public static bool DeletePreset(Rect rect, ColourPreset preset)
    {
        rect.x += 5f;
        if (!Widgets.ButtonImage(rect, TexButton.Delete))
        {
            return false;
        }
            
        ModSettings.RemovePreset(preset);
        return true;
    }
    public static bool DeletePreset(Rect rect, DecorationPreset preset)
    {
        rect.x += 5f;
        if (!Widgets.ButtonImage(rect, TexButton.Delete))
        {
            return false;
        }
            
        ModSettings.RemovePreset(preset);
        return true;
    }
    
    private static readonly Dictionary<ThingDef, bool> verbModifyingDefCache = new();

    public static bool CanModifyVerbs(ThingWithComps equipment)
    {
        if (equipment?.def == null)
        {
            return false;
        }

        if (verbModifyingDefCache.TryGetValue(equipment.def, out var cached))
        {
            return cached;
        }

        var result = false;
        if (!equipment.def.comps.NullOrEmpty())
        {
            foreach (var compProperties in equipment.def.comps)
            {
                if (compProperties?.compClass == null)
                {
                    continue;
                }

                if (typeof(Comp_AmmoChanger).IsAssignableFrom(compProperties.compClass)
                    || typeof(CompWeaponDecoration).IsAssignableFrom(compProperties.compClass))
                {
                    result = true;
                    break;
                }
            }
        }

        verbModifyingDefCache.Add(equipment.def, result);
        return result;
    }

    private static class CompDefCache<T> where T : ThingComp
    {
        public static readonly ConcurrentDictionary<ThingDef, bool> Cache = new();
    }

    /// <summary>
    /// Whether the def lists a comp of type T (or a subclass), memoised per def so hot paths can
    /// skip the per-instance comp scan on things that can never carry it. Safe to call from the
    /// parallel render phase.
    /// </summary>
    public static bool DefHasComp<T>(ThingDef def) where T : ThingComp
    {
        if (def == null)
        {
            return false;
        }

        return CompDefCache<T>.Cache.GetOrAdd(def, static thingDef =>
        {
            if (thingDef.comps.NullOrEmpty())
            {
                return false;
            }

            foreach (var compProperties in thingDef.comps)
            {
                if (compProperties?.compClass != null && typeof(T).IsAssignableFrom(compProperties.compClass))
                {
                    return true;
                }
            }

            return false;
        });
    }
    
    private static readonly Dictionary<(Color, Color?, Color?, int), Texture2D> colourPreviewCache = new();

    public static Texture2D ThreeColourPreview(Color primaryColor, Color? secondaryColor, Color? tertiaryColor, int colorAmount)
    {
        var cacheKey = (primaryColor, secondaryColor, tertiaryColor, colorAmount);
        
        if (colourPreviewCache.TryGetValue(cacheKey, out var cachedPreview) && cachedPreview != null)
        {
            return cachedPreview;
        }

        var texture2D = new Texture2D(3,3)
        {
            name = "SolidColorTex-" + primaryColor + secondaryColor
        };
        texture2D.SetPixel(0, 0, primaryColor);
        texture2D.SetPixel(0, 1, primaryColor);
        texture2D.SetPixel(0, 2, primaryColor);

        var secondRowPixel = primaryColor;
        var thirdRowPixel = primaryColor;
        
        if (secondaryColor.HasValue && secondaryColor.Value.a != 0 && colorAmount > 1)
        {
            secondRowPixel = secondaryColor.Value;
            thirdRowPixel = secondaryColor.Value;
        }
        if (tertiaryColor.HasValue && tertiaryColor.Value.a != 0 && colorAmount > 2)
        {
            thirdRowPixel = tertiaryColor.Value;
        }
        
        texture2D.SetPixel(1, 0, secondRowPixel);
        texture2D.SetPixel(1, 1, secondRowPixel);
        texture2D.SetPixel(1, 2, secondRowPixel);
        texture2D.SetPixel(2, 0, thirdRowPixel);
        texture2D.SetPixel(2, 1, thirdRowPixel);
        texture2D.SetPixel(2, 2, thirdRowPixel);
        texture2D.wrapMode = TextureWrapMode.Clamp;
        texture2D.filterMode = FilterMode.Point;
        texture2D.Apply();

        colourPreviewCache[cacheKey] = texture2D;
        return texture2D;
    }
    
    public static bool ContainsAllItems<T>(this IEnumerable<T> a, IEnumerable<T> b)
    {
        return !b.Except(a).Any();
    }
    
    public static bool HasCompAssignable(this ThingDef def, System.Type compType)
    {
        if (def?.comps == null || compType == null)
        {
            return false;
        }

        foreach (var comp in def.comps)
        {
            if (comp?.compClass != null && compType.IsAssignableFrom(comp.compClass))
            {
                return true;
            }
        }

        return false;
    }
    
    public static string ValueToString(StatDef stat, float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
    {
        if (!finalized)
        {
            var text = val.ToStringByStyle(stat.ToStringStyleUnfinalized, numberSense);
            if (numberSense != ToStringNumberSense.Factor && !stat.formatStringUnfinalized.NullOrEmpty())
            {
                text = string.Format(stat.formatStringUnfinalized, text);
            }
            return text;
        }
        var text2 = val.ToStringByStyle(stat.toStringStyle, numberSense);
        if (numberSense != ToStringNumberSense.Factor && !stat.formatString.NullOrEmpty())
        {
            text2 = string.Format(stat.formatString, text2);
        }
        return text2;
    }

    public static bool HasMultiColorThing(this Pawn pawn)
    {
        if (pawn.apparel?.WornApparel != null)
        {
            if (pawn.apparel.WornApparel.Any(apparel => apparel.HasComp<CompMultiColor>()))
            {
                return true;
            }
        }

        if (pawn.equipment?.Primary?.GetComp<CompMultiColor>() != null)
        {
            return true;
        }

        return false;
    }

    private static void SetupMultiColorCustomization(ThingWithComps thing, Dictionary<ColourPresetDef, ColorSelectionType> finalSelection, Pawn pawn)
    {
        var colorComp = thing.GetComp<CompMultiColor>();
        if (colorComp == null)
        {
            return;
        }
        
        var selection =
            finalSelection
                .Where(col => 
                    col.Value == ColorSelectionType.TryMatch 
                    && col.Key.appliesTo.Contains(thing.def.defName)).FirstOrFallback(new KeyValuePair<ColourPresetDef, ColorSelectionType>());

        if (selection.Key == null)
        {
            selection = finalSelection
                .Where(col => 
                    col.Value == ColorSelectionType.Default).FirstOrFallback();
        }

        if (selection.Key == null)
        {
            Log.Warning("Tried to give " + pawn.kindDef + " default colored clothe, but is not setup correctly");
            return;
        }
            
        colorComp.SetColors(selection.Key);
        colorComp.SetOriginals();
        colorComp.InitialSet = true;
    }
    
    private static void SetupDecorationCustomization(ThingWithComps thing, DefModExtension_PawnKindCustomization pawnModExtension)
    {
        var decoComp = thing.GetComp<CompDecorative>();
        if (decoComp == null || pawnModExtension == null)
        {
            return;
        }
        
        if (pawnModExtension.extraDecorationPreset.TryGetValue(thing.def, out var preset))
        {
            decoComp.LoadFromPreset(preset, free: true);
        }
        if (pawnModExtension.extraDecorations.TryGetValue(thing.def, out var decos))
        {
            decoComp.ApplyDecorationsFromList([..decos], free: true);
        }
    }
    
    private static Dictionary<ColourPresetDef, ColorSelectionType> ResolveColorSelection(Pawn pawn, out DefModExtension_PawnKindCustomization pawnModExtension)
    {
        var factionSelection = pawn.Faction?.def?.GetModExtension<DefModExtension_PawnKindCustomization>()?.defaultColorSelection;
        pawnModExtension = pawn.kindDef?.GetModExtension<DefModExtension_PawnKindCustomization>();
        var pawnKindSelection = pawnModExtension?.defaultColorSelection;

        if (!pawnKindSelection.NullOrEmpty())
        {
            return pawnKindSelection;
        }

        return !factionSelection.NullOrEmpty() ? factionSelection : null;
    }

    /// <summary>
    /// Applies the pawn kind or faction defaults to one item. Each customizable item runs this from
    /// its own first equip, so the pawn's whole outfit is covered without re-walking it per item.
    /// </summary>
    public static void SetupCustomizationForThing(Pawn pawn, ThingWithComps thing, bool setupMultiColor, bool setupDecoration)
    {
        if (pawn == null || thing == null)
        {
            return;
        }

        var finalSelection = ResolveColorSelection(pawn, out var pawnModExtension);
        if (finalSelection == null)
        {
            return;
        }

        if (setupMultiColor)
        {
            SetupMultiColorCustomization(thing, finalSelection, pawn);
        }

        if (setupDecoration)
        {
            SetupDecorationCustomization(thing, pawnModExtension);
        }
    }

    public static void SetupCustomizationForPawn(Pawn pawn, bool setupMultiColor, bool setupDecoration)
    {
        var finalSelection = ResolveColorSelection(pawn, out var pawnModExtension);
        if (finalSelection == null)
        {
            return;
        }
        
        if (pawn.apparel?.WornApparel != null)
        {
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (setupMultiColor)
                {
                    SetupMultiColorCustomization(apparel, finalSelection, pawn);
                }
                
                if (setupDecoration)
                {
                    SetupDecorationCustomization(apparel, pawnModExtension);
                }
            }
        }
        
        var equipment = pawn.equipment?.PrimaryEq?.parent;
        var colorCompWeap = equipment?.GetComp<CompMultiColor>();
        if (colorCompWeap != null)
        {
            if (setupMultiColor)
            {
                SetupMultiColorCustomization(equipment, finalSelection, pawn);
            }

            if (setupDecoration)
            {
                SetupDecorationCustomization(equipment, pawnModExtension);
            }
        }
    }

    public static int CountBuildingColonistOfDef(this ListerBuildings listerBuildings, ThingDef def)
    {
        return listerBuildings.AllBuildingsColonistOfDef(def).Count;
    }
    
    private static Color MenuSectionBGFillColor = new ColorInt(42, 43, 44).ToColor;
    private static Color MenuSectionBGBorderColor = new ColorInt(135, 135, 135).ToColor;
    
    public static void DrawColoredMenuSection(Rect rect, Color? menuFillColor, Color? borderColor)
    {
        GUI.color = menuFillColor ?? MenuSectionBGFillColor;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = borderColor ?? MenuSectionBGBorderColor;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
    }
    
    public static void TextFieldWithHorizontalSlider(ref Rect textRect, ref float value, ref string textBuffer, string label, float minVal, float maxVal, bool asIntValue = false)
    {
        var sliderRect = textRect.TakeTopPart(textRect.height/2);
        
        var valX = Widgets.TextArea(textRect, textBuffer);
        
        textBuffer = valX;
        if (float.TryParse(valX, out var newValx))
        {
            value = newValx;
        }
        
        var newSliderValue = Widgets.HorizontalSlider(sliderRect, value , minVal, maxVal, true, label);

        if (asIntValue)
        {
            newSliderValue = Mathf.RoundToInt(newSliderValue);
        }
        
        if (!Mathf.Approximately(newSliderValue, value))
        {
            value = newSliderValue;
            textBuffer = newSliderValue.ToString();
        }
    }

    public static void AddAbilities(this Pawn pawn, List<AbilityDef> vanillaAbilities, List<VEF.Abilities.AbilityDef> VEFAbilities)
    {
        if (!vanillaAbilities.NullOrEmpty())
        {
            foreach (var ability in vanillaAbilities)
            {
                pawn.abilities.GainAbility(ability);
            }
        }
            
        if (!VEFAbilities.NullOrEmpty())
        {
            var comp = pawn.GetComp<CompAbilities>();
            if (comp != null)
            {
                foreach (var ability in VEFAbilities)
                {
                    comp.GiveAbility(ability);
                }
            }
        }
    }
    
    public static void RemoveAbilities(this Pawn pawn, List<AbilityDef> vanillaAbilities, List<VEF.Abilities.AbilityDef> VEFAbilities)
    {
        if (vanillaAbilities != null)
        {
            foreach (var ability in vanillaAbilities)
            {
                pawn.abilities.RemoveAbility(ability);
            }
        }
            
        if (VEFAbilities != null)
        {
            var comp = pawn.GetComp<CompAbilities>();
            if (comp != null)
            {
                foreach (var ability in VEFAbilities)
                {
                    comp.LearnedAbilities.RemoveWhere(learnedAbility => learnedAbility.def == ability);
                }
            }
        }
    }
}