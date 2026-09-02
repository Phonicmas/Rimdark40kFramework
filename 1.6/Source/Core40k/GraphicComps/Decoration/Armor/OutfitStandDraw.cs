using UnityEngine;
using Verse;

namespace Core40k;

/// <summary>
/// One quad an outfit stand draws on top of the apparel it is displaying. Layer, offset and scale
/// are in stand space - a provider converts them out of pawn space before building this.
/// </summary>
public readonly struct OutfitStandDraw
{
    public readonly Graphic graphic;
    public readonly float layer;
    public readonly Vector3 offset;
    public readonly Vector3 scale;
    public readonly float angle;
    public readonly bool flipMesh;
    public readonly bool flipMaterial;

    public OutfitStandDraw(Graphic graphic, float layer, Vector3 offset, Vector3 scale, float angle, bool flipMesh, bool flipMaterial)
    {
        this.graphic = graphic;
        this.layer = layer;
        this.offset = offset;
        this.scale = scale;
        this.angle = angle;
        this.flipMesh = flipMesh;
        this.flipMaterial = flipMaterial;
    }
}
