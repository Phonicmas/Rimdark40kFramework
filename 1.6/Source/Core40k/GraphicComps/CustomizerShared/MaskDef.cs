using System.Collections.Generic;
using Verse;

namespace Core40k;

public class MaskDef : Def
{
    public string maskPath = null;
    public List<string> appliesTo = [];
    public int sortOrder = 1;
    public bool setsNull = false;
    public AppliesToKind appliesToKind = AppliesToKind.Thing;
    public int colorAmount = 1;
    public bool useBodyTypes = false;
    public ShaderTypeDef shaderType = null;
    
    public override void ResolveReferences()
    {
        base.ResolveReferences();
        if (shaderType == null && !setsNull)
        {
            Log.Warning("Shader type is not defined in MaskDef: " + defName + ", using thing parent shader.");
        }
    }
}

public enum AppliesToKind
{
    None,
    Thing,
    ExtraDecoration,
    All,
}