using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Core40k;

public class PawnRenderNode_FlagEdit : PawnRenderNode_Apparel
{
    private static GameComponent_CoreUtils CoreUtils => Current.Game?.GetComponent<GameComponent_CoreUtils>();
    
    public PawnRenderNode_FlagEdit(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
    {
    }

    private string ModifyPathByFlags(List<TextureFlag> textureFlags, Pawn pawn)
    {
        var path = string.Empty;
        if (textureFlags.NullOrEmpty())
        {
            return path;
        }
        foreach (var flag in textureFlags)
        {
            var flagExpansion = ModifyByFlag(flag, pawn);

            path += flagExpansion;
        }
        return path;
    }

    protected virtual string ModifyByFlag(TextureFlag flag, Pawn pawn)
    {
        if (flag.pathExpansion == string.Empty)
        {
            return string.Empty;
        }

        if (flag.thingActivator != null && pawn?.apparel?.WornApparel != null
            && !pawn.apparel.WornApparel.Select(apparel1 => apparel1.def).Contains(flag.thingActivator)
            && pawn.equipment?.Primary?.def != flag.thingActivator)
        {
            return string.Empty;
        }

        if (flag.hediffActivator != null && pawn?.health?.hediffSet?.hediffs != null
            && !pawn.health.hediffSet.hediffs.Select(hediff1 => hediff1.def).Contains(flag.hediffActivator))
        {
            return string.Empty;
        }

        if (flag.geneActivator != null && pawn?.genes?.GenesListForReading != null
            && !pawn.genes.GenesListForReading.Where(gene1 => gene1.Active).Select(gene1 => gene1.def).Contains(flag.geneActivator))
        {
            return string.Empty;
        }

        if (flag.gizmoActivated && !GizmoToggledOn(pawn))
        {
            return string.Empty;
        }

        return flag.pathExpansion;
    }

    private bool GizmoToggledOn(Pawn pawn)
    {
        if (pawn == null || apparel == null)
        {
            return false;
        }

        var coreUtils = CoreUtils;
        return coreUtils != null && coreUtils.cachedGizmoToggles.TryGetValue((pawn, apparel), out var toggledOn) && toggledOn;
    }

    public override Graphic GraphicFor(Pawn pawn)
    {
        var defMod = apparel?.def?.GetModExtension<DefModExtension_TextureFlags>();
        if (defMod == null)
        {
            return base.GraphicFor(pawn);
        }
        
        var flags = defMod.textureFlags.Where(t=> !t.shouldAddInsteadOfSwap).OrderBy(t => t.order).ToList();
        
        var modifiedPath = ModifyPathByFlags(flags, pawn);
        
        var path = Props.texPath;
        
        if (Props.bodyTypeGraphicPaths != null)
        {
            foreach (var bodyTypeGraphicPath in Props.bodyTypeGraphicPaths)
            {
                if (pawn.story.bodyType != bodyTypeGraphicPath.bodyType)
                {
                    continue;
                }
                path = bodyTypeGraphicPath.texturePath;
                break;
            }
        }
        
        string maskPath = null;
        var multiColor = apparel.GetComp<CompMultiColor>();
        if (multiColor?.MaskDef != null)
        {
            if (defMod.ShouldExpandMaskPath(multiColor.MaskDef, Props.texSeed))
            {
                maskPath = multiColor.MaskDef.maskPath + defMod.GetExpansionPathByIdentifier(Props.texSeed);
            }
        }
        
        if (defMod.ShouldExpandBasePath(Props.texSeed))
        {
            path += modifiedPath;
            if (maskPath != null)
            {
                maskPath += modifiedPath;
            }
        }
        
        return MultiColorUtils.GetGraphic<Graphic_Multi>(path, Core40kDefOf.BEWH_CutoutThreeColor.Shader, Props.drawSize, 
            multiColor?.DrawColor ?? apparel.DrawColor, 
            multiColor?.DrawColorTwo ?? apparel.DrawColorTwo, 
            multiColor?.DrawColorThree ?? apparel.DrawColorTwo, apparel.def.graphicData, maskPath);
    }
    
    protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
    {
        var defMod = apparel.def.GetModExtension<DefModExtension_TextureFlags>();
        if (defMod == null)
        {
            yield return GraphicFor(pawn);
            yield break;
        }
        
        var flags = defMod.textureFlags.Where(t=> t.shouldAddInsteadOfSwap).OrderBy(t => t.order);

        foreach (var flag in flags)
        {
            //TODO: Make graphic for added stuff and yield return;
        }
        
        yield return GraphicFor(pawn);
    }
}