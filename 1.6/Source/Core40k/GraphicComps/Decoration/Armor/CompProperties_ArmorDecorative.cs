using UnityEngine;

namespace Core40k;

public class CompProperties_Decorative : CompProperties_DecorationBase
{
    public DecorativeType decorativeType = DecorativeType.Body;

    //Unworn look: the worn south texture at groundDrawSize with decorations laid out as on a pawn.
    //useIconOnGround keeps the item icon instead and maps decorations onto it proportionally.
    public bool useIconOnGround = false;
    public bool drawRenderNodesOnGround = false;
    public float groundDrawSize = 1.2f;

    public bool drawDecorationsOnGround = true;
    //Icon mode only.
    public Vector3 groundDecorationOffset = Vector3.zero;
    public float groundDecorationScale = 1f;
    
    public CompProperties_Decorative()
    {
        compClass = typeof(CompDecorative);
    }
}

public enum DecorativeType
{
    Body = 0,
    Head = 1,
}

