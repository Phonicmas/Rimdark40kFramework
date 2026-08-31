using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Core40k;

/// <summary>
/// Add to a BodyTypeDef via &lt;modExtensions&gt; to tell Core40k which existing bodytype's
/// textures to borrow when a custom bodytype has none of its own.
/// </summary>
public class DefModExtension_BodyTypeTextureFallback : DefModExtension
{
    public List<BodyTypeDef> fallbackTo = [];
}

public static class BodyTypeUtils
{
    /// <summary>
    /// Suffix used by Female Apparel Variants (tiagocc0.FemaleApparelVariants) for gender-specific
    /// art, appended after the bodytype: Apparel/PowerArmor/PowerArmor_Hulk_Female_south.png.
    /// Core40k resolves it natively so the convention keeps working whether or not FAV is installed
    /// - see FemaleApparelVariantsCompat for why we cannot let FAV resolve it for our apparel.
    /// </summary>
    public const string FemaleVariantSuffix = "_Female";

    private static readonly Dictionary<(string, BodyTypeDef, Gender), string> pathCache = new();
    private static readonly Dictionary<(string, BodyTypeDef, Gender), string> maskPathCache = new();
    private static readonly Dictionary<BodyTypeDef, List<BodyTypeDef>> chainCache = new();

    /// <summary>
    /// The best existing texture for this path and gender - the "_Female" variant when the pawn is
    /// female and that art exists, the plain path when it does not, null when neither is there.
    /// </summary>
    private static string GenderedPath(string path, Gender gender)
    {
        if (path.NullOrEmpty())
        {
            return null;
        }

        if (gender == Gender.Female)
        {
            var femalePath = path + FemaleVariantSuffix;
            if (TextureExists(femalePath))
            {
                return femalePath;
            }
        }

        return TextureExists(path) ? path : null;
    }

    /// <summary>Never returns null. Use instead of pawn.story.bodyType.</summary>
    public static BodyTypeDef SafeBodyType(Pawn pawn, BodyTypeDef preferred = null)
    {
        return preferred ?? pawn?.story?.bodyType ?? BodyTypeDefOf.Male;
    }

    /// <summary>True if a Graphic_Multi or Graphic_Single could be built from this path.</summary>
    public static bool TextureExists(string path)
    {
        if (path.NullOrEmpty())
        {
            return false;
        }
        return ContentFinder<Texture2D>.Get(path + "_south", false) != null
               || ContentFinder<Texture2D>.Get(path + "_north", false) != null
               || ContentFinder<Texture2D>.Get(path + "_east", false) != null
               || ContentFinder<Texture2D>.Get(path + "_west", false) != null
               || ContentFinder<Texture2D>.Get(path, false) != null;
    }

    private static List<BodyTypeDef> vanillaBodyTypes;
    private static List<BodyTypeDef> VanillaBodyTypes => vanillaBodyTypes ??=
    [
        BodyTypeDefOf.Male,
        BodyTypeDefOf.Female,
        BodyTypeDefOf.Thin,
        BodyTypeDefOf.Fat,
        BodyTypeDefOf.Hulk,
    ];

    /// <summary>The bodytypes to try, in order, when looking for textures for this bodytype.</summary>
    public static List<BodyTypeDef> FallbackChainFor(BodyTypeDef bodyType)
    {
        bodyType ??= BodyTypeDefOf.Male;

        if (chainCache.TryGetValue(bodyType, out var cached))
        {
            return cached;
        }

        var chain = new List<BodyTypeDef> { bodyType };

        //1. Whatever the bodytype itself declares.
        var declared = bodyType.GetModExtension<DefModExtension_BodyTypeTextureFallback>()?.fallbackTo;
        if (!declared.NullOrEmpty())
        {
            foreach (var def in declared.Where(def => def != null && !chain.Contains(def)))
            {
                chain.Add(def);
            }
        }

        //2. Keyword bias - a bodytype called "XYZ_Hulk_Big" almost certainly wants Hulk art.
        foreach (var vanilla in VanillaBodyTypes)
        {
            if (vanilla != null
                && !chain.Contains(vanilla)
                && bodyType.defName.IndexOf(vanilla.defName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                chain.Add(vanilla);
            }
        }

        //3. Nearest vanilla bodytype by graphic scale, so a slim custom body prefers Thin over Hulk.
        var scale = bodyType.bodyGraphicScale;
        foreach (var vanilla in VanillaBodyTypes
                     .Where(v => v != null && !chain.Contains(v))
                     .OrderBy(v => (v.bodyGraphicScale - scale).sqrMagnitude))
        {
            chain.Add(vanilla);
        }

        chainCache[bodyType] = chain;
        return chain;
    }

    /// <summary>
    /// basePath + "_" + a bodytype that actually has textures, or basePath if none do. Pass the
    /// wearer's gender to pick up "_Female" variants of that art where a modder has supplied them.
    /// </summary>
    public static string BodyTypedPath(string basePath, BodyTypeDef bodyType, Gender gender = Gender.None)
    {
        if (basePath.NullOrEmpty())
        {
            return basePath;
        }

        bodyType ??= BodyTypeDefOf.Male;

        var key = (basePath, bodyType, gender);
        if (pathCache.TryGetValue(key, out var cachedPath))
        {
            return cachedPath;
        }

        string result = null;
        foreach (var candidate in FallbackChainFor(bodyType))
        {
            var candidatePath = GenderedPath(basePath + "_" + candidate.defName, gender);
            if (candidatePath == null)
            {
                continue;
            }
            result = candidatePath;
            if (candidate != bodyType)
            {
                Log.WarningOnce(
                    $"[Core40k] No texture at {basePath}_{bodyType.defName}; falling back to {candidatePath}.",
                    (basePath + bodyType.defName).GetHashCode());
            }
            break;
        }

        if (result == null)
        {
            result = GenderedPath(basePath, gender) ?? basePath + "_" + bodyType.defName;
            Log.WarningOnce(
                $"[Core40k] No bodytype texture found for {basePath} (bodytype {bodyType.defName}).",
                (basePath + bodyType.defName + "none").GetHashCode());
        }

        pathCache[key] = result;
        return result;
    }

    /// <summary>
    /// Same resolution for masks - returns null when no mask variant exists at all, so callers can
    /// pass null straight through to GraphicDatabase rather than a broken path.
    /// </summary>
    public static string BodyTypedMaskPath(string baseMaskPath, BodyTypeDef bodyType, Gender gender = Gender.None)
    {
        if (baseMaskPath.NullOrEmpty())
        {
            return baseMaskPath;
        }

        bodyType ??= BodyTypeDefOf.Male;

        var key = (baseMaskPath, bodyType, gender);
        if (maskPathCache.TryGetValue(key, out var cachedPath))
        {
            return cachedPath;
        }

        string result = null;
        foreach (var candidate in FallbackChainFor(bodyType))
        {
            result = GenderedPath(baseMaskPath + "_" + candidate.defName, gender);
            if (result != null)
            {
                break;
            }
        }

        result ??= GenderedPath(baseMaskPath, gender);

        maskPathCache[key] = result;
        return result;
    }

    /// <summary>
    /// Does this bodytype - or anything it falls back to - appear in the list? Lets
    /// appliesToBodyTypes keep working for modded bodytypes.
    /// </summary>
    public static bool MatchesAny(BodyTypeDef bodyType, List<BodyTypeDef> list, out BodyTypeDef matched)
    {
        matched = null;
        if (list.NullOrEmpty())
        {
            return false;
        }

        foreach (var candidate in FallbackChainFor(bodyType))
        {
            if (!list.Contains(candidate))
            {
                continue;
            }
            matched = candidate;
            return true;
        }

        return false;
    }
}
