using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Core40k;

public static class MultiColorUtils
{
    private static readonly HashSet<Graphic> ownGraphics = [];

    /// <summary>
    /// True if this graphic was built here and therefore already carries the
    /// _DrawColor/_DrawColorTwo/_DrawColorThree shader parameters. Body-rescaling mods rebuild
    /// graphics through a GraphicDatabase.Get overload that drops those parameters; this is how we
    /// tell one of ours apart from one of theirs.
    /// </summary>
    public static bool IsOwnGraphic(Graphic graphic)
    {
        return graphic != null && ownGraphics.Contains(graphic);
    }

    public static T GetGraphic<T>(string path, Shader shader, Vector2 drawSize, Color colorOne, Color colorTwo, Color colorThree, GraphicData data, string maskPath = null) where T : Graphic
    {
        var shaderParameter1 = new ShaderParameter();
        var traverse = Traverse.Create(shaderParameter1);
        traverse.Field("name").SetValue("_DrawColor");
        traverse.Field("type").SetValue(1);
        traverse.Field("value").SetValue(new Vector4(colorOne.r, colorOne.g, colorOne.b, colorOne.a));
        
        var shaderParameter2 = new ShaderParameter();
        traverse = Traverse.Create(shaderParameter2);
        traverse.Field("name").SetValue("_DrawColorTwo");
        traverse.Field("type").SetValue(1);
        traverse.Field("value").SetValue(new Vector4(colorTwo.r, colorTwo.g, colorTwo.b, colorTwo.a));
        
        var shaderParameter3 = new ShaderParameter();
        traverse = Traverse.Create(shaderParameter3);
        traverse.Field("name").SetValue("_DrawColorThree");
        traverse.Field("type").SetValue(1);
        traverse.Field("value").SetValue(new Vector4(colorThree.r, colorThree.g, colorThree.b, colorThree.a));
        
        var shaderParameters = new List<ShaderParameter>
        {
            shaderParameter1,
            shaderParameter2,
            shaderParameter3
        };
        
        var graphic = GraphicDatabase.Get(typeof(T), path, shader, drawSize, colorOne, colorTwo, data, shaderParameters, maskPath) as T;

        if (graphic != null)
        {
            //GraphicDatabase holds these forever anyway, so tracking them leaks nothing.
            ownGraphics.Add(graphic);
            return graphic;
        }

        //GraphicDatabase.Get swallows exceptions and hands back BaseContent.BadGraphic, which is a
        //Graphic_Single - so the cast above silently yields null when T is Graphic_Multi. Returning
        //null from here poisons ApparelGraphicRecord and NREs deep inside the pawn render tree,
        //so build a graphic of the right type instead.
        Log.ErrorOnce(
            $"[Core40k] Failed to build {typeof(T).Name} at '{path}' (mask '{maskPath ?? "none"}'). Using fallback graphic.",
            ("Core40kGraphicFail" + path + maskPath).GetHashCode());

        return GraphicDatabase.Get(typeof(T), path, shader, drawSize, colorOne, colorTwo, data, null, maskPath) as T
               ?? GraphicDatabase.Get(typeof(T), BaseContent.BadTexPath, ShaderDatabase.Cutout, drawSize, colorOne, colorTwo, null, null) as T;
    }
}