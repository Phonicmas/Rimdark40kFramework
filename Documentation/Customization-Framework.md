# Customization Framework

The customization framework is the shared dialog/tab plumbing behind the "style at a styling
station" feature. It doesn't do anything on its own — it hosts three concrete features that
plug into it: [multi-colour recolouring](#multi-colour-recolouring), swappable
**alternate base textures**, and [decorations](Decorations). If you only need one of those
features on a piece of apparel or a weapon, you still go through the pieces on this page to wire
it up.

## The pieces

| Class | Role |
|---|---|
| `CustomizationTabDef` | A `Def` describing one tab in the customization dialog: a label and a `tabDrawerClass` (a `Type` deriving from `CustomizerTabDrawer`). |
| `CustomizerTabDrawer` | Base class for a tab's drawing/lifecycle logic: `Setup`, `DrawTab`, `OnAccept`, `OnReset`, `OnClose`. |
| `DefModExtension_AvailableDrawerTabDefs` | Put on a `ThingDef` (apparel or weapon). `tabDefs` is the list of `CustomizationTabDef`s that item offers in the dialog. **This is how you opt an item into the dialog at all.** |
| `Dialog_CustomizeApparel` / `Dialog_CustomizeWeapon` | The actual window. Built from every worn apparel item (or the equipped weapon) carrying `DefModExtension_AvailableDrawerTabDefs`, unioning all their tabs into one tabbed dialog. |
| `DefModExtension_AllowColoringOfThings` | Put on a `ThingDef` to add a right-click float menu option ("Change apparel decoration" / "Change weapon decoration") that opens the dialog. |

### Opting an item into the dialog

```xml
<ThingDef ParentName="ApparelMakeableBase">
  <defName>BEWH_Apparel_PowerArmor</defName>
  <!-- ... -->
  <comps>
    <li Class="Core40k.CompProperties_MultiColor">
      <colorMaskAmount>3</colorMaskAmount>
    </li>
  </comps>
  <modExtensions>
    <li Class="Core40k.DefModExtension_AvailableDrawerTabDefs">
      <tabDefs>
        <li>BEWH_ArmorColoring</li>
        <li>BEWH_ArmorDecoration</li>
        <li>BEWH_ArmorAlternateBaseForms</li>
      </tabDefs>
    </li>
    <li Class="Core40k.DefModExtension_AllowColoringOfThings">
      <allowColoringOfApparel>true</allowColoringOfApparel>
    </li>
  </modExtensions>
</ThingDef>
```

The six built-in `CustomizationTabDef`s (`1.6/Defs/Customization/CustomizationTabDefs.xml`) are:

| defName | Tab | Drawer |
|---|---|---|
| `BEWH_ArmorColoring` | Armor Coloring | `ArmorColoringTab` |
| `BEWH_ArmorDecoration` | Armor Decoration | `ArmorDecorationTab` |
| `BEWH_ArmorAlternateBaseForms` | Alternate base textures | `ArmorAlternateTextureTab` |
| `BEWH_WeaponColoring` | Weapon Coloring | `WeaponColoringTab` |
| `BEWH_WeaponDecoration` | Weapon Decoration | `WeaponDecorationTab` |
| `BEWH_WeaponAlternateBaseForms` | Alternate base textures | `WeaponAlternateTextureTab` |

You only list the tabs relevant to the item — an item with no `CompDecorative`/`CompWeaponDecoration`
shouldn't list a decoration tab.

> **Legacy path, don't use it:** `CompProperties_MultiColor.tabDefs` (an `[Obsolete]` field) used
> to be how tabs were declared, directly on the color comp. `CompProperties_MultiColor.ResolveReferences`
> logs a warning if it's still set. Always use `DefModExtension_AvailableDrawerTabDefs` instead.

## Multi-colour recolouring

`CompMultiColor` (`CompProperties_MultiColor`) gives an apparel/weapon up to three independently
recolourable channels plus an optional `MaskDef` (a second texture that defines which pixels each
colour channel affects).

```xml
<li Class="Core40k.CompProperties_MultiColor">
  <colorMaskAmount>3</colorMaskAmount>          <!-- 1, 2, or 3 usable colour channels -->
  <defaultPrimaryColor>(0.5,0.5,0.5)</defaultPrimaryColor>
  <defaultSecondaryColor>(0.2,0.2,0.2)</defaultSecondaryColor>
  <defaultTertiaryColor>(0.8,0.1,0.1)</defaultTertiaryColor>
</li>
```

- `colorMaskAmount == 3` makes the graphic render with the framework's own three-colour shader,
  `Core40kDefOf.BEWH_CutoutThreeColor` (`ShaderTypeDef`); anything less uses the item's normal
  shader (or the item's own `graphicData.shaderType`).
- If no `defaultPrimaryColor` is set, apparel/weapons made from stuff default to the stuff's
  colour; otherwise white.
- `MaskDef` (see below) is picked per-item in the coloring tab, not fixed on the comp — a `MaskDef`
  is a separate texture (`maskPath`) whose `appliesTo` list of defNames (or `appliesToKind: All`)
  determines which items it's offered on. `useBodyTypes: true` on a `MaskDef` appends
  `_<BodyTypeDef>` to the mask path so a mask can vary by body type, matching how the base
  graphic itself is looked up.
- `ColourPresetDef` defines named colour swatches players can one-click apply
  (`primaryColour`/`secondaryColour`/`tertiaryColour`, `appliesTo` a list of defNames or
  `appliesToKind: Armor|Weapon|All`).

## Alternate base textures

`CompAlternateTexture` (`CompProperties_AlternateTexture`, no extra fields of its own) lets a
piece of apparel or a weapon swap its entire base texture/draw size/colours for one of several
alternates, defined as `AlternateBaseFormDef` (a `DecorationDef` subclass — see
[Decorations](Decorations) for the fields it inherits):

```xml
<Core40k.AlternateBaseFormDef>
  <defName>BEWH_AltForm_PowerArmor_Damaged</defName>
  <label>battle-worn</label>
  <appliesTo><li>BEWH_Apparel_PowerArmor</li></appliesTo>
  <drawnTextureIconPath>Things/Apparel/PowerArmor/PowerArmor_Damaged</drawnTextureIconPath>
  <newDrawSize>(1.5,1.5)</newDrawSize>
  <newPrimaryColor>(0.3,0.3,0.3)</newPrimaryColor>
  <incompatibleMaskDefs>
    <li>BEWH_SomeMask</li>
  </incompatibleMaskDefs>
</Core40k.AlternateBaseFormDef>
```

Selecting an alternate form: resets any `MaskDef` the item is currently using if that mask is in
`incompatibleMaskDefs`; applies `newPrimaryColor`/`newSecondaryColor`/`newTertiaryColor` onto the
item's `CompMultiColor` if present; removes any attached decorations incompatible with it (either
listed in the alternate's own `incompatibleDecorations`, or any decoration flagged
`isIncompatibleWithBaseTexture` when reverting to the default/no alternate form); and swaps in
`givesAbilities`/`givesVFEAbilities` for the abilities the *previous* selection granted.
Because `AlternateBaseFormDef` extends `DecorationDef`, an alternate form itself can carry
`statOffsets`/`statFactors` — those surface under the `BEWH_AlternateTextureOffsets` /
`BEWH_AlternateTextureFactors` stat categories on the item's info card.

## Applying default looks per pawnkind/faction

`DefModExtension_PawnKindCustomization` goes on a `PawnKindDef` or a `FactionDef`:

```xml
<li Class="Core40k.DefModExtension_PawnKindCustomization">
  <defaultColorSelection>
    <BEWH_ColourPreset_UltramarinesBlue>TryMatch</BEWH_ColourPreset_UltramarinesBlue>
    <BEWH_ColourPreset_Default>Default</BEWH_ColourPreset_Default>
  </defaultColorSelection>
  <extraDecorations>
    <BEWH_Apparel_PowerArmor>
      <li>BEWH_Deco_UltramarinesChapterIcon</li>
    </BEWH_Apparel_PowerArmor>
  </extraDecorations>
</li>
```

`Core40kUtils.SetupCustomizationForPawn(pawn, setupMultiColor, setupDecoration)` reads the
pawnkind's extension (falling back to the faction's) and, for each worn/equipped item: picks a
`ColourPresetDef` whose `appliesTo` matches that item's defName (`TryMatch`), or the first
`Default`-flagged preset otherwise, and applies it to `CompMultiColor`; and/or applies
`extraDecorations`/`extraDecorationPreset` to `CompDecorative`. `CompDecorative.Notify_Equipped`
calls this automatically (once per pawn, tracked by `pawnKindDefSetupDone`) the first time a pawn
equips apparel carrying `CompDecorative` — you don't normally need to call it yourself unless
you're spawning gear onto a pawn outside the normal equip path.

## Troubleshooting

- **`BEWH_CutoutThreeColor` needs an actual mask.** Unlike vanilla's `Cutout`/`CutoutComplex`
  shaders, the framework's three-colour shader (used automatically whenever `colorMaskAmount == 3`
  on `CompMultiColor`, see above) does not degrade gracefully without a real mask texture present.
  If an item using it renders wrong (or not at all), check that it actually has a `MaskDef` — its
  own `defaultMask`, or one the player can select — pointing at a texture that exists.
- **"Could not find texture ..._<BodyType>" in the log.** Base graphics, masks, and decoration art
  are all looked up with a body-type (and, with Female Apparel Variants installed, gender) suffix
  appended to the configured path. If a texture with that suffix can't be found, the usual cause
  isn't a missing file — it's the relevant `appliesTo` list on the item's `DecorationDef`/
  `MaskDef`/`AlternateBaseFormDef` being empty, missing the item's defName, or pointing at the
  wrong one, so the framework goes looking for art under the wrong assumptions. Double-check
  `appliesTo` before assuming the texture itself is missing.
