using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Core40k;

public static class MultiColorUtils
{
    private static readonly HashSet<Graphic> ownGraphics = [];

    private static readonly FieldInfo ShaderParameterName = AccessTools.Field(typeof(ShaderParameter), "name");
    private static readonly FieldInfo ShaderParameterType = AccessTools.Field(typeof(ShaderParameter), "type");
    private static readonly FieldInfo ShaderParameterValue = AccessTools.Field(typeof(ShaderParameter), "value");

    //GraphicRequest and MaterialRequest compare shaderParameters by reference, so the same colour
    //triple has to hand GraphicDatabase the same list instance or every call builds a new graphic.
    private static readonly Dictionary<(Color, Color, Color), List<ShaderParameter>> shaderParametersByColour = new();

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
        var shaderParameters = ShaderParametersFor(colorOne, colorTwo, colorThree);
        
        var graphic = GraphicDatabase.Get(typeof(T), path, shader, drawSize, colorOne, colorTwo, data, shaderParameters, maskPath) as T;

        if (graphic != null)
        {
            ownGraphics.Add(graphic);
            return graphic;
        }
        
        Log.ErrorOnce(
            $"[Core40k] Failed to build {typeof(T).Name} at '{path}' (mask '{maskPath ?? "none"}'). Using fallback graphic.",
            ("Core40kGraphicFail" + path + maskPath).GetHashCode());

        return GraphicDatabase.Get(typeof(T), path, shader, drawSize, colorOne, colorTwo, data, null, maskPath) as T
               ?? GraphicDatabase.Get(typeof(T), BaseContent.BadTexPath, ShaderDatabase.Cutout, drawSize, colorOne, colorTwo, null, null) as T;
    }

    /// <summary>
    /// Returns the shared _DrawColor/_DrawColorTwo/_DrawColorThree parameter list for a colour triple,
    /// building it on first use.
    /// </summary>
    private static List<ShaderParameter> ShaderParametersFor(Color colorOne, Color colorTwo, Color colorThree)
    {
        var key = (colorOne, colorTwo, colorThree);
        if (shaderParametersByColour.TryGetValue(key, out var shaderParameters))
        {
            return shaderParameters;
        }

        shaderParameters =
        [
            MakeVectorParameter("_DrawColor", colorOne),
            MakeVectorParameter("_DrawColorTwo", colorTwo),
            MakeVectorParameter("_DrawColorThree", colorThree)
        ];
        shaderParametersByColour.Add(key, shaderParameters);

        return shaderParameters;
    }

    private static ShaderParameter MakeVectorParameter(string name, Color color)
    {
        var shaderParameter = new ShaderParameter();
        ShaderParameterName.SetValue(shaderParameter, name);
        ShaderParameterType.SetValue(shaderParameter, Enum.ToObject(ShaderParameterType.FieldType, 1));
        ShaderParameterValue.SetValue(shaderParameter, new Vector4(color.r, color.g, color.b, color.a));
        return shaderParameter;
    }
}
