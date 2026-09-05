using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

public class PawnRenderNodeWorker_AlternateFormCompatible : PawnRenderNodeWorker
{
    private sealed class CompHolder
    {
        public CompAlternateTexture comp;
    }

    //A node's apparel never changes, so the comp is looked up once per node. The table drops the
    //entry with the node, and GetValue is safe from the parallel render phase.
    private static readonly ConditionalWeakTable<PawnRenderNode, CompHolder> compCache = new();

    private static CompAlternateTexture AlternateTextureFor(PawnRenderNode node)
    {
        if (node?.apparel == null)
        {
            return null;
        }

        return compCache.GetValue(node, static n => new CompHolder { comp = n.apparel.GetComp<CompAlternateTexture>() }).comp;
    }

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        var alternateTexture = AlternateTextureFor(node);
        if (alternateTexture?.CurrentAlternateBaseForm == null)
        {
            return base.OffsetFor(node, parms, out pivot);
        }
        
        var curAltForm = alternateTexture.CurrentAlternateBaseForm;
        
        var anchorOffset = Vector3.zero;
        pivot = PivotFor(node, parms);
        if (curAltForm.drawData != null)
        {
            if (curAltForm.drawData.useBodyPartAnchor)
            {
                if (node.bodyPart == null)
                {
                    Log.ErrorOnce($"Attempted to use a body part anchor but no body-part record has been assigned to this node {node}", node.GetHashCode());
                    return anchorOffset;
                }
                foreach (BodyTypeDef.WoundAnchor item in PawnDrawUtility.FindAnchors(parms.pawn, node.bodyPart))
                {
                    if (PawnDrawUtility.AnchorUsable(parms.pawn, item, parms.facing))
                    {
                        PawnDrawUtility.CalcAnchorData(parms.pawn, item, parms.facing, out anchorOffset, out var _);
                    }
                }
            }
            var vector = curAltForm.drawData.OffsetForRot(parms.facing);
            if (curAltForm.drawData.scaleOffsetByBodySize && parms.pawn?.story?.bodyType != null)
            {
                var bodyGraphicScale = parms.pawn.story.bodyType.bodyGraphicScale;
                var num = (bodyGraphicScale.x + bodyGraphicScale.y) / 2f;
                vector *= num;
            }
            anchorOffset += vector;
        }
        anchorOffset += node.DebugOffset;
        if (!parms.flags.FlagSet(PawnRenderFlags.Portrait) && node.TryGetAnimationOffset(parms, out var offset))
        {
            anchorOffset += offset;
        }
        return anchorOffset;
    }
    
    protected override Vector3 PivotFor(PawnRenderNode node, PawnDrawParms parms)
    {
        var alternateTexture = AlternateTextureFor(node);
        if (alternateTexture?.CurrentAlternateBaseForm == null)
        {
            return base.PivotFor(node, parms);
        }
        
        var curAltForm = alternateTexture.CurrentAlternateBaseForm;
        
        var result = Vector3.zero;
        if (curAltForm.drawData != null)
        {
            result -= (curAltForm.drawData.PivotForRot(parms.facing) - DrawData.PivotCenter).ToVector3();
        }
        if (node.tree.TryGetAnimationPartForNode(node, out var animationPart))
        {
            result = (animationPart.pivot - DrawData.PivotCenter).ToVector3();
        }
        if (node.debugPivotOffset != DrawData.PivotCenter)
        {
            result.x += node.debugPivotOffset.x - DrawData.PivotCenter.x;
            result.z += node.debugPivotOffset.y - DrawData.PivotCenter.y;
        }
        return result;
    }
}