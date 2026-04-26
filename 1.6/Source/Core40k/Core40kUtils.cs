using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Verse;

namespace Core40k;

[StaticConstructorOnStartup]
public static class Core40kUtils
{
    public const string MankindsFinestPackageId = "Phonicmas.RimDark.MankindsFinest";
        
    public static readonly Texture2D FlippedIconTex = ContentFinder<Texture2D>.Get("UI/Decoration/flipIcon");
    public static readonly Texture2D ScrollForwardIcon = ContentFinder<Texture2D>.Get ("UI/Misc/ScrollForwardIcon");
    public static readonly Texture2D ScrollBackwardIcon = ContentFinder<Texture2D>.Get ("UI/Misc/ScrollBackwardIcon");
    
    public static readonly Graphic_Multi EmptyMultiGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>("UI/EmptyImage");
    
    private static Core40kModSettings modSettings = null;
    public static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();

    public static readonly Color RequirementMetColour = Color.white;
    public static readonly Color RequirementNotMetColour = new Color(1f, 0.0f, 0.0f, 0.8f);
    
    public static ExtraDecorationDef GetArmorDecoDefFromString(string defName)
    {
        return DefDatabase<ExtraDecorationDef>.GetNamed(defName);
    }
    
    public static WeaponDecorationDef GetWeaponDecoDefFromString(string defName)
    {
        return DefDatabase<WeaponDecorationDef>.GetNamed(defName);
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
    public static bool DeletePreset(Rect rect, ExtraDecorationPreset preset)
    {
        rect.x += 5f;
        if (!Widgets.ButtonImage(rect, TexButton.Delete))
        {
            return false;
        }
            
        ModSettings.RemovePreset(preset);
        return true;
    }
        
    //Colour Preview
    public static Texture2D ThreeColourPreview(Color primaryColor, Color? secondaryColor, Color? tertiaryColor, int colorAmount)
    {
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

        return texture2D;
    }
    
    public static bool ContainsAllItems<T>(this IEnumerable<T> a, IEnumerable<T> b)
    {
        return !b.Except(a).Any();
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
    
    public static void SetupColorsForPawn(Pawn pawn)
    {
        var factionSelection = pawn.Faction?.def?.GetModExtension<DefModExtension_DefaultMultiColor>()?.defaultColorSelection;
        var pawnKindSelection = pawn.kindDef?.GetModExtension<DefModExtension_DefaultMultiColor>()?.defaultColorSelection;
        
        Dictionary<ColourPresetDef, ColorSelectionType> finalSelection;
        if (!pawnKindSelection.NullOrEmpty())
        {
            finalSelection = pawnKindSelection;
        }
        else if (!factionSelection.NullOrEmpty())
        {
            finalSelection = factionSelection;
        }
        else
        {
            return;
        }

        if (pawn.apparel?.WornApparel != null)
        {
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var selection =
                    finalSelection
                        .Where(col => 
                            col.Value == ColorSelectionType.TryMatch 
                            && col.Key.appliesTo.Contains(apparel.def.defName)).FirstOrFallback(new KeyValuePair<ColourPresetDef, ColorSelectionType>());

                if (selection.Key == null)
                {
                    selection = finalSelection
                        .Where(col => 
                            col.Value == ColorSelectionType.Default).FirstOrFallback();
                }

                if (selection.Key == null)
                {
                    Log.Warning("Tried to give " + pawn.kindDef + " default colored clothe, but is not setup correctly");
                    continue;
                }
                
                var comp = apparel.GetComp<CompMultiColor>();
                if (comp == null)
                {
                    continue;
                }
            
                comp.SetColors(selection.Key);
                comp.SetOriginals();
                comp.InitialSet = true;
            }
        }
        
        var equipment = pawn.equipment?.PrimaryEq?.parent;
        if (equipment != null && equipment.HasComp<CompMultiColor>())
        {
            var selection =
                finalSelection
                    .Where(col => 
                        col.Value == ColorSelectionType.TryMatch 
                        && col.Key.appliesTo.Contains(equipment.def.defName)).FirstOrFallback();

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
            
            var comp = equipment.GetComp<CompMultiColor>();
            
            comp.SetColors(selection.Key);
            comp.SetOriginals();
            comp.InitialSet = true;
        }
    }

    public static int CountBuildingColonistOfDef(this ListerBuildings listerBuildings, ThingDef def)
    {
        return listerBuildings.AllBuildingsColonistOfDef(def).Count;
    }
}