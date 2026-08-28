using HarmonyLib;
using Verse;

namespace Core40k;

/// <summary>
/// Body-rescaling mods - Varied Body Sizes (Mlie.VariedBodySizes) and anything with the same shape -
/// postfix PawnRenderTree.TrySetupGraphIfNeeded and swap PawnRenderNode.primaryGraphic for a copy at a
/// new draw size, rebuilt through the GraphicDatabase.Get overload that takes no shaderParameters.
/// A Graphic does not carry those parameters, so they cannot know to preserve ours: the rebuilt
/// graphic keeps path, mask and colours one and two, but loses _DrawColor/_DrawColorTwo/
/// _DrawColorThree, and masked apparel then renders with the shader's defaults.
///
/// We run last on the same method and rebuild anything they replaced, keeping their draw size and
/// restoring our shader parameters. With no such mod present every node exits on the first check.
/// </summary>
[HarmonyPatch(typeof(PawnRenderTree), "TrySetupGraphIfNeeded")]
[HarmonyPriority(Priority.Last)]
[HarmonyAfter("Mlie.VariedBodySizes")]
public static class RestoreMultiColorAfterRescalePatch
{
    private static readonly AccessTools.FieldRef<PawnRenderNode, Graphic> PrimaryGraphicRef =
        AccessTools.FieldRefAccess<PawnRenderNode, Graphic>("primaryGraphic");

    public static void Postfix(PawnRenderTree __instance)
    {
        if (__instance?.rootNode == null)
        {
            return;
        }

        RestoreNode(__instance.rootNode);
    }

    private static void RestoreNode(PawnRenderNode node)
    {
        if (node == null)
        {
            return;
        }

        if (node.children != null)
        {
            foreach (var child in node.children)
            {
                RestoreNode(child);
            }
        }

        var apparel = node.apparel;
        if (apparel == null)
        {
            return;
        }

        var current = PrimaryGraphicRef(node);

        //Null, or still a graphic we built - nothing has rescaled it.
        if (current == null || MultiColorUtils.IsOwnGraphic(current))
        {
            return;
        }

        var multiColor = apparel.GetComp<CompMultiColor>();
        var alternateTexture = apparel.GetComp<CompAlternateTexture>();
        if (multiColor == null && alternateTexture == null)
        {
            return;
        }

        //Rebuild at whatever draw size the other mod settled on, with our shader parameters back.
        var rebuilt = MultiColorUtils.GetGraphic<Graphic_Multi>(
            current.path,
            current.Shader,
            current.drawSize,
            multiColor?.DrawColor ?? apparel.DrawColor,
            multiColor?.DrawColorTwo ?? apparel.DrawColorTwo,
            multiColor?.DrawColorThree ?? apparel.DrawColorTwo,
            null,
            current.maskPath);

        if (rebuilt != null && rebuilt != current)
        {
            PrimaryGraphicRef(node) = rebuilt;
        }
    }
}
