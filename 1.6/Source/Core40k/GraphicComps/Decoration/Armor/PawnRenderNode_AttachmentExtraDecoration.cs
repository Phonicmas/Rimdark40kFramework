using UnityEngine;
using Verse;

namespace Core40k;

public class PawnRenderNode_AttachmentExtraDecoration : PawnRenderNode
{
    public CompDecorativeBase decorativeComp;
    public DecorationDef decorationDef;
    
    public PawnRenderNode_AttachmentExtraDecoration(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
    {
    }

    public override Graphic GraphicFor(Pawn pawn)
    {
        var propsMulti = (PawnRenderNodePropertiesMultiColor)Props;

        var texPath = propsMulti.texPath;
        var shader = propsMulti.shaderTypeDef;
        var maskPath = propsMulti.maskDef?.maskPath ?? string.Empty;
        
        var bodyType = propsMulti.bodyType ?? pawn?.story?.bodyType;
        var useBodyType = propsMulti.useBodyType && bodyType != null;

        if (useBodyType)
        {
            texPath = BodyTypeUtils.BodyTypedPath(texPath, bodyType);
        }

        var additionalMaskPath = string.Empty;
        if (maskPath != string.Empty)
        {
            maskPath += additionalMaskPath;
            if (useBodyType)
            {
                maskPath = BodyTypeUtils.BodyTypedMaskPath(maskPath, bodyType) ?? maskPath;
            }
        }
        else
        {
            maskPath = propsMulti.texPath + additionalMaskPath;
            if (useBodyType)
            {
                maskPath = BodyTypeUtils.BodyTypedMaskPath(maskPath, bodyType) ?? maskPath;
            }
            maskPath += "_mask";
        }
        
        return MultiColorUtils.GetGraphic<Graphic_Multi>(texPath, shader.Shader, propsMulti.drawSize, propsMulti.color ?? Color.white, propsMulti.colorTwo ?? Color.white, propsMulti.colorThree ?? Color.white, null, maskPath);

    }
}