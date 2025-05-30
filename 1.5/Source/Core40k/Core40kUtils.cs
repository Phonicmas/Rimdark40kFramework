﻿using UnityEngine;
using Verse;

namespace Core40k;

[StaticConstructorOnStartup]
public static class Core40kUtils
{
    public const string MankindsFinestPackageId = "Phonicmas.RimDark.MankindsFinest";
        
    public static readonly Texture2D FlippedIconTex = ContentFinder<Texture2D>.Get("UI/Decoration/flipIcon");
        
    private static Core40kModSettings modSettings = null;
    public static Core40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();

    public static ExtraDecorationDef GetDefFromString(string defName)
    {
        return DefDatabase<ExtraDecorationDef>.GetNamed(defName);
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
    public static Texture2D TwoColourPreview(Color primaryColor, Color secondaryColor)
    {
        var texture2D = new Texture2D(2, 2)
        {
            name = "SolidColorTex-" + primaryColor + secondaryColor
        };
        texture2D.SetPixel(0, 0, primaryColor);
        texture2D.SetPixel(0, 1, primaryColor);
        texture2D.SetPixel(1, 0, secondaryColor);
        texture2D.SetPixel(1, 1, secondaryColor);
        texture2D.wrapMode = TextureWrapMode.Clamp;
        texture2D.filterMode = FilterMode.Bilinear;
        texture2D.Apply();
            
        return texture2D;
    }
    public static Texture2D ColourPreview(Color primaryColor)
    {
        var texture2D = new Texture2D(1, 1)
        {
            name = "SolidColorTex-" + primaryColor
        };
        texture2D.SetPixel(0, 0, primaryColor);
        texture2D.Apply();
            
        return texture2D;
    }
}