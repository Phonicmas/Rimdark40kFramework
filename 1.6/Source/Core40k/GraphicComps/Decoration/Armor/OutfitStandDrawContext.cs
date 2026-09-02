using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Core40k;

/// <summary>
/// Everything a provider needs about one piece of apparel held by an outfit stand, plus the lists
/// its quads go into. Built once per apparel per cache rebuild and shared by all providers.
/// </summary>
public class OutfitStandDrawContext
{
    //Pawn space layers of the two apparel roots in the humanlike render tree. Vanilla's own outfit
    //stand code uses the same two numbers to translate apparel drawData layers into stand space.
    public const float BodyApparelLayer = 20f;
    public const float HeadApparelLayer = 70f;

    private readonly List<OutfitStandDraw>[] bodyDraws;
    private readonly List<OutfitStandDraw>[] headDraws;
    private readonly List<OutfitStandDraw>[] overHeadDraws;
    private readonly float[] apparelLayers;
    private readonly Action skipHead;

    public Building_OutfitStand Stand { get; }

    public Apparel Apparel { get; }

    public BodyTypeDef StandBodyType { get; }

    /// <summary>The stand's bodytype, unless this apparel forces one of its own.</summary>
    public BodyTypeDef ApparelBodyType { get; }

    public bool IsChild { get; }

    public float BodySizeFactor { get; }

    /// <summary>Overhead or EyeCover, the two layers the stand draws in head space.</summary>
    public bool Headgear { get; }

    /// <summary>The apparel's own layer in stand space, before its drawData is applied.</summary>
    public int ApparelBaseLayer { get; }

    /// <summary>The highest layer vanilla gives a headgear quad on this stand.</summary>
    public float HeadApparelTopLayer { get; }

    public OutfitStandDrawContext(Building_OutfitStand stand, Apparel apparel, BodyTypeDef standBodyType, bool isChild, float bodySizeFactor, bool headgear, int apparelBaseLayer, float headApparelTopLayer, float[] apparelLayers, List<OutfitStandDraw>[] bodyDraws, List<OutfitStandDraw>[] headDraws, List<OutfitStandDraw>[] overHeadDraws, Action skipHead)
    {
        Stand = stand;
        Apparel = apparel;
        StandBodyType = standBodyType;
        ApparelBodyType = apparel.def.GetModExtension<DefModExtension_ForcesBodyType>()?.forcedBodyType ?? standBodyType;
        IsChild = isChild;
        BodySizeFactor = bodySizeFactor;
        Headgear = headgear;
        ApparelBaseLayer = apparelBaseLayer;
        HeadApparelTopLayer = headApparelTopLayer;

        this.apparelLayers = apparelLayers;
        this.bodyDraws = bodyDraws;
        this.headDraws = headDraws;
        this.overHeadDraws = overHeadDraws;
        this.skipHead = skipHead;
    }

    /// <summary>Hides the stand's head and everything drawn in head space.</summary>
    public void SkipHead()
    {
        skipHead();
    }

    /// <summary>The apparel's stand layer for this rotation, with its own drawData applied.</summary>
    public float ApparelLayerForRot(Rot4 rot)
    {
        return apparelLayers[rot.AsInt];
    }

    /// <summary>
    /// Translates a render node layer authored in pawn space into stand space, keeping its distance
    /// from the node it hangs off. Not clamped, so a node authored behind the body stays behind it.
    /// Vanilla doubles that distance for apparel drawData, but attachment nodes are authored far
    /// from the node base and PawnRenderUtility.AltitudeForLayer tops out at 100, so doubling would
    /// saturate and collapse layers that need to stay apart.
    /// </summary>
    public float StandLayerFor(float pawnLayer, bool headSpace)
    {
        return ApparelBaseLayer + (pawnLayer - (headSpace ? HeadApparelLayer : BodyApparelLayer));
    }

    /// <summary>
    /// True when a body space node is authored above the pawn's head apparel root, and so draws over
    /// a helmet on a pawn. The stand draws headgear a whole altitude layer above everything else, so
    /// such a node cannot stay in the body band - see AddOverHead.
    /// </summary>
    public bool DrawsOverHeadApparel(float pawnLayer)
    {
        return pawnLayer > HeadApparelLayer;
    }

    /// <summary>Layer for a quad added through AddOverHead, measured above the topmost headgear.</summary>
    public float OverHeadLayerFor(float pawnLayer)
    {
        return HeadApparelTopLayer + (pawnLayer - HeadApparelLayer);
    }

    public void AddBody(Rot4 rot, OutfitStandDraw draw)
    {
        bodyDraws[rot.AsInt].Add(draw);
    }

    public void AddHead(Rot4 rot, OutfitStandDraw draw)
    {
        headDraws[rot.AsInt].Add(draw);
    }

    /// <summary>
    /// A quad drawn in the stand's headgear altitude band but at the body's position, for body space
    /// nodes that belong over the helmet. Drawn whether or not the head is skipped.
    /// </summary>
    public void AddOverHead(Rot4 rot, OutfitStandDraw draw)
    {
        overHeadDraws[rot.AsInt].Add(draw);
    }
}
