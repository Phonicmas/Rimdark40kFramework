using System.Collections.Generic;
using UnityEngine;

namespace Core40k;

public class AlternateBaseFormDef : DecorationDef
{
    public List<WeaponDecorationDef> incompatibleWeaponDecorations = [];
    public List<ExtraDecorationDef> incompatibleArmorDecorations = [];
    public List<MaskDef> incompatibleMaskDefs = [];

    public Color? newPrimaryColor;
    public Color? newSecondaryColor;
    public Color? newTertiaryColor;

    public Vector2? newDrawSize;
}