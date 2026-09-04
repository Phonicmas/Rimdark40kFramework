using Verse;

namespace Core40k;

/// <summary>
/// Draws the framework's own attachment render nodes - backpacks and shoulder pads - on an outfit
/// stand, in the armour's colours. Texture flags are deliberately not applied: every flag is
/// conditioned on a wearer or on a per-pawn gizmo toggle, so the base path is the unworn look.
/// </summary>
public class OutfitStandDrawProvider_MultiColorAttachments : OutfitStandDrawProvider
{
    public override int Order => 10;

    public override void CollectDraws(OutfitStandDrawContext context)
    {
        var apparel = context.Apparel;

        var multiColor = apparel.GetComp<CompMultiColor>();
        if (multiColor == null)
        {
            return;
        }

        var nodeProperties = apparel.def.apparel?.RenderNodeProperties;
        if (nodeProperties.NullOrEmpty())
        {
            return;
        }

        var textureFlags = apparel.def.GetModExtension<DefModExtension_TextureFlags>();

        foreach (var props in nodeProperties)
        {
            var shoulderPad = typeof(PawnRenderNodeWorker_AttachmentShoulderPad).IsAssignableFrom(props.workerClass);
            if (!shoulderPad && !typeof(PawnRenderNodeWorker_AttachmentBackpack).IsAssignableFrom(props.workerClass))
            {
                continue;
            }

            if (props.texPath.NullOrEmpty())
            {
                continue;
            }

            var graphic = BuildGraphic(apparel, multiColor, textureFlags, props);
            if (graphic == null)
            {
                continue;
            }

            for (var i = 0; i < 4; i++)
            {
                var rot = new Rot4(i);

                //PawnRenderNodeWorker_AttachmentShoulderPad hides the pad facing north and south.
                if (shoulderPad && !rot.IsHorizontal)
                {
                    continue;
                }

                AddNodeDraw(context, props, graphic, rot);
            }
        }
    }

    /// <summary>The same resolution PawnRenderNode_FlagEdit.GraphicFor does, with no wearer.</summary>
    internal static Graphic BuildGraphic(Thing apparel, CompMultiColor multiColor, DefModExtension_TextureFlags textureFlags, PawnRenderNodeProperties props)
    {
        string maskPath = null;
        if (multiColor.MaskDef != null && textureFlags != null && textureFlags.ShouldExpandMaskPath(multiColor.MaskDef, props.texSeed))
        {
            maskPath = multiColor.MaskDef.maskPath + textureFlags.GetExpansionPathByIdentifier(props.texSeed);
        }

        return MultiColorUtils.GetGraphic<Graphic_Multi>(
            props.texPath,
            Core40kDefOf.BEWH_CutoutThreeColor.Shader,
            props.drawSize,
            multiColor.DrawColor,
            multiColor.DrawColorTwo,
            multiColor.DrawColorThree,
            apparel.def.graphicData,
            maskPath);
    }
}
