using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Core40k;

public class DynamicPawnRenderNodeSetup_DecorativeAddons : DynamicPawnRenderNodeSetup
{
    public override bool HumanlikeOnly => true;
    
    public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
    {
        if (pawn.apparel == null || pawn.apparel.WornApparelCount == 0)
        {
            yield break;
        }

        var decorativeApparels = pawn.apparel.WornApparel.Where(apparel => apparel.HasComp<CompDecorative>()).ToList();
        
        var pawnBodyType = pawn.story.bodyType;
        
        foreach (var decorativeApparel in decorativeApparels)
        {
            var decorativeComp = decorativeApparel.GetComp<CompDecorative>();
            
            if (decorativeApparel.def.HasModExtension<DefModExtension_ForcesBodyType>())
            {
                pawnBodyType = decorativeApparel.def.GetModExtension<DefModExtension_ForcesBodyType>().forcedBodyType ?? pawnBodyType;
            }
            foreach (var decoration in decorativeComp.Decorations)
            {
                if (decoration.Key is not ExtraDecorationDef decorationDef)
                {
                    continue;
                }
                PawnRenderNodePropertiesMultiColor pawnRenderNodeProperty;
                PawnRenderNode node;
                switch (decorativeComp.Props.decorativeType)
                {
                    case DecorativeType.Head:
                        pawnRenderNodeProperty = MakeHeadProps(decorationDef, decoration.Value, decorativeComp);
                        node = (tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.ApparelHead, out node) ? node : null) ??
                                              (tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Head, out node) ? node : null);
                        break;
                    case DecorativeType.Body:
                        pawnRenderNodeProperty = MakeBodyProps(decorationDef, decoration.Value, decorativeComp, pawnBodyType);
                        if (decorationDef.drawInHeadSpace)
                        {
                            node = (tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.ApparelHead, out node) ? node : null) ??
                                   (tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Head, out node) ? node : null);
                        }
                        else
                        {
                            node = (tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.ApparelBody, out node) ? node : null) ??
                                   (tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Body, out node) ? node : null);
                        }
                        break;
                    default:
                        continue;
                }
                
                var pawnRenderNode = (PawnRenderNode_AttachmentExtraDecoration)Activator.CreateInstance(typeof(PawnRenderNode_AttachmentExtraDecoration), pawn, pawnRenderNodeProperty, tree);
                
                pawnRenderNode.decorativeComp = decorativeComp;
                pawnRenderNode.decorationDef = decoration.Key;
                
                yield return (pawnRenderNode, node);
            }
        }
    }

    private PawnRenderNodePropertiesMultiColor MakeBodyProps(ExtraDecorationDef decorationDef, DecorationSettings decorationSettings, CompDecorative decorativeComp, BodyTypeDef pawnBodyType)
    {
        var pawnRenderNodeProperty = MakeBaseProps(decorationDef, decorationSettings, decorativeComp);
        pawnRenderNodeProperty.parentTagDef = decorationDef.drawInHeadSpace ? PawnRenderNodeTagDefOf.Head : PawnRenderNodeTagDefOf.Body;
        pawnRenderNodeProperty.workerClass = typeof(PawnRenderNodeWorker_AttachmentExtraDecorationBody);
        pawnRenderNodeProperty.bodyType = pawnBodyType;
        pawnRenderNodeProperty.useBodyType = decorationDef.appliesToBodyTypes.Contains(pawnBodyType);

        return pawnRenderNodeProperty;
    }
    
    private PawnRenderNodePropertiesMultiColor MakeHeadProps(ExtraDecorationDef decorationDef, DecorationSettings decorationSettings, CompDecorative decorativeComp)
    {
        var pawnRenderNodeProperty = MakeBaseProps(decorationDef, decorationSettings, decorativeComp);
        pawnRenderNodeProperty.parentTagDef = PawnRenderNodeTagDefOf.Head;
        pawnRenderNodeProperty.workerClass = typeof(PawnRenderNodeWorker_AttachmentExtraDecorationHead);

        return pawnRenderNodeProperty;
    }
    
    private PawnRenderNodePropertiesMultiColor MakeBaseProps(ExtraDecorationDef decorationDef, DecorationSettings decorationSettings, CompDecorative decorativeComp)
    {
        var pawnRenderNodeProperty = new PawnRenderNodePropertiesMultiColor
        {
            nodeClass = typeof(PawnRenderNode_AttachmentExtraDecoration),
            texPath = decorationDef.drawnTextureIconPath,
            shaderTypeDef = decorationSettings.maskDef?.shaderType ?? decorationDef.shaderType,
            drawData = decorationDef.drawData,
            drawSize = decorationDef.drawSize,
            flipGraphic = decorationSettings.Flipped,
            color = decorationSettings.Color,
            colorTwo = decorationSettings.ColorTwo,
            colorThree = decorationSettings.ColorThree,
            maskDef = decorationSettings.maskDef,
        };

        return pawnRenderNodeProperty;
    }
}