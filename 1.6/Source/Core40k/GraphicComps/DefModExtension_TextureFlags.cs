using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

public class DefModExtension_TextureFlags : DefModExtension
{
    public List<TextureFlag> textureFlags = [];
    
    public List<MaskExpansion> maskExpansions = [];

    public bool ShouldExpandMaskPath(MaskDef maskDef, int identifier)
    {
        return Enumerable.Any(maskExpansions, maskExpansion => maskExpansion.identifier == identifier && maskExpansion.maskDefsWithExpansion.Contains(maskDef));
    }

    public bool ShouldExpandBasePath(int identifier)
    {
        foreach (var textureFlag in textureFlags)
        {
            if (textureFlag.maskIdentifiers.Contains(identifier))
            {
                return true;
            }
        }

        return false;
    }

    public string GetExpansionPathByIdentifier(int identifier)
    {
        foreach (var maskExpansion in maskExpansions)
        {
            if (maskExpansion.identifier == identifier)
            {
                return maskExpansion.pathExpansionOnMask;
            }
        }
        
        return string.Empty;
    }
}

public class TextureFlag
{
    public TextureFlag(){}
    
    public int order = 0;
    public List<int> maskIdentifiers = [];
    public string pathExpansion = string.Empty;
    
    public bool shouldAddInsteadOfSwap = false;
    public bool hideThing = false;

    //Texture swapped in for an apparel this flag hides. The default belongs to one content pack, so
    //framework logic no longer assumes it: a pack that ships its own blank can point at that.
    [NoTranslate]
    public string hideTexPath = "Things/Armor/Imperium/PowerArmor/CommonIcons/BEWH_None";
    
    public ThingDef thingActivator = null;
    public HediffDef hediffActivator = null;
    public GeneDef geneActivator = null;

    public bool gizmoActivated = false;
    public string gizmoOnText = "On";
    public string gizmoOffText = "Off";
}

public class MaskExpansion
{
    public MaskExpansion(){}

    public int identifier = 0;
    public string pathExpansionOnMask = string.Empty;
    public List<MaskDef> maskDefsWithExpansion = [];
}