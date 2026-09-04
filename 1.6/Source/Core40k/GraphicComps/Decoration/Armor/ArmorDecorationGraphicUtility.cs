using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Resolves the graphic for an armour decoration that is not being worn - on an outfit stand or on
/// the ground. The same resolution PawnRenderNode_AttachmentExtraDecoration.GraphicFor does, with no
/// wearer so no gendered art.
/// </summary>
public static class ArmorDecorationGraphicUtility
{
    public static Graphic BuildGraphic(ExtraDecorationDef decoration, DecorationSettings settings, bool useBodyType, BodyTypeDef bodyType)
    {
        var shader = settings.maskDef?.shaderType ?? decoration.shaderType;
        if (shader == null)
        {
            return null;
        }

        var texPath = decoration.drawnTextureIconPath;
        var maskPath = settings.maskDef?.maskPath ?? string.Empty;

        if (useBodyType)
        {
            texPath = BodyTypeUtils.BodyTypedPath(texPath, bodyType);
        }

        if (maskPath != string.Empty)
        {
            if (useBodyType)
            {
                maskPath = BodyTypeUtils.BodyTypedMaskPath(maskPath, bodyType) ?? maskPath;
            }
        }
        else
        {
            maskPath = decoration.drawnTextureIconPath;
            if (useBodyType)
            {
                maskPath = BodyTypeUtils.BodyTypedMaskPath(maskPath, bodyType) ?? maskPath;
            }
            maskPath += "_mask";
        }

        return MultiColorUtils.GetGraphic<Graphic_Multi>(
            texPath,
            shader.Shader,
            decoration.drawSize,
            settings.Color,
            settings.ColorTwo,
            settings.ColorThree,
            null,
            maskPath);
    }
}
