using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Optional integration with Female Apparel Variants (tiagocc0.FemaleApparelVariants). Nothing here
/// is referenced unless that mod is loaded, and every lookup is by name, so the framework carries no
/// dependency on it.
///
/// FAV side, for reference: FemaleApparelVariants.PawnRenderNode_Apparel_GraphicsFor_Patch declares
///
///     static bool Prefix(PawnRenderNode_Apparel node, Pawn pawn, ref IEnumerable&lt;Graphic&gt; __result)
///
/// on Verse.PawnRenderNode_Apparel.GraphicsFor, and it returns false - it builds its own
/// Graphic_Multi through GraphicDatabase with ShaderDatabase.Cutout/CutoutComplex and no mask, then
/// skips the original. That means ApparelGraphicRecordGetter.TryGetGraphicApparel never runs, and
/// with it neither branch of ApparelGraphicPatch (CompMultiColor masks, the three-colour shader,
/// CompAlternateTexture, forced body types). The visible symptom is armour rendering with flat
/// colours and no mask whenever FAV is installed.
///
/// The fix is not to fight FAV over the graphic but to stand it down for the apparel Core40k owns:
/// this prefix runs first, makes FAV's prefix return true without executing, and lets vanilla
/// GraphicsFor call through to our own patches. Female variants are not lost in the process -
/// BodyTypeUtils resolves the exact same "&lt;path&gt;_&lt;bodytype&gt;_Female" textures itself, so
/// Core40k apparel picks up female art whether or not FAV is installed at all.
/// </summary>
public static class FemaleApparelVariantsCompat
{
    private const string PatchTypeName = "FemaleApparelVariants.PawnRenderNode_Apparel_GraphicsFor_Patch";
    private const string PatchMethodName = "Prefix";

    public static bool Active { get; private set; }

    /// <summary>
    /// Called from Core40kMod's constructor, after PatchAll. Attribute-driven patches cannot be used
    /// here - PatchAll resolves their targets eagerly and would throw when FAV is absent.
    /// </summary>
    public static void Apply(Harmony harmony)
    {
        // The type resolving at all is the check: if FAV is not loaded, it is not there.
        var patchType = AccessTools.TypeByName(PatchTypeName);
        if (patchType == null)
        {
            return;
        }

        var target = AccessTools.Method(patchType, PatchMethodName);
        if (target == null || !SignatureMatches(target))
        {
            Log.Warning("[RimDark Framework] Female Apparel Variants is present but its GraphicsFor prefix does not look the way we expect, so it will keep overwriting Core40k apparel graphics - masks and alternate textures will not show. This usually means FAV changed its patch layout.");
            return;
        }

        harmony.Patch(target, new HarmonyMethod(typeof(FemaleApparelVariantsCompat), nameof(YieldToCore40k))
        {
            priority = Priority.First,
        });

        Active = true;
    }

    /// <summary>
    /// We inject by position (__0), so refuse to patch anything that is not still
    /// "bool Prefix(PawnRenderNode_Apparel, ...)" rather than let Harmony throw at patch time.
    /// </summary>
    private static bool SignatureMatches(MethodInfo target)
    {
        if (target.ReturnType != typeof(bool))
        {
            return false;
        }

        var parameters = target.GetParameters();
        return parameters.Length > 0 && parameters[0].ParameterType == typeof(PawnRenderNode_Apparel);
    }

    /// <summary>
    /// Setting __result to true is the whole point: a prefix that returns false makes Harmony hand
    /// back default(bool) for the skipped method, and false out of FAV's prefix would mean "skip
    /// vanilla GraphicsFor" - exactly the opposite of what we want.
    /// </summary>
    private static bool YieldToCore40k(PawnRenderNode_Apparel __0, ref bool __result)
    {
        if (!Core40kOwnsGraphic(__0?.apparel))
        {
            return true;
        }

        __result = true;
        return false;
    }

    /// <summary>
    /// True when this apparel's graphic is built by Core40k rather than by vanilla, so no other mod
    /// should be resolving its texture path, shader or mask.
    /// </summary>
    public static bool Core40kOwnsGraphic(Apparel apparel)
    {
        if (apparel == null)
        {
            return false;
        }

        //ApparelGraphicPatch territory.
        if (apparel.HasComp<CompMultiColor>() || apparel.HasComp<CompAlternateTexture>())
        {
            return true;
        }

        //ApparelGraphicPatch forced body type branch - the forcing apparel itself...
        if (apparel.def.HasModExtension<DefModExtension_ForcesBodyType>())
        {
            return true;
        }

        //...and everything else the same pawn wears, which vanilla then draws on the forced body.
        if (apparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead)
        {
            return false;
        }

        var worn = apparel.Wearer?.apparel?.WornApparel;
        if (worn == null)
        {
            return false;
        }

        foreach (var t in worn)
        {
            if (t.def.HasModExtension<DefModExtension_ForcesBodyType>())
            {
                return true;
            }
        }

        return false;
    }
}
