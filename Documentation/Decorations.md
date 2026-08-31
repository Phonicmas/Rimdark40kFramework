# Decorations

Decorations are small attachable pieces layered on top of apparel or a weapon — trinkets, purity
seals, decals, sights, wraps, a bayonet, an underbarrel launcher. They're one of the tabs hosted
by the [Customization Framework](Customization-Framework.md) (`BEWH_ArmorDecoration` /
`BEWH_WeaponDecoration`), and can also be applied directly through def extensions without the
player ever opening the dialog (starting pawnkind loadouts, presets).

## Shared base: `DecorationDef`

Both `ExtraDecorationDef` (armor) and `WeaponDecorationDef` derive from `Core40k.DecorationDef`,
which carries the fields common to every decoration:

| Field | Type | Meaning |
|---|---|---|
| `iconPath` / `drawnTextureIconPath` | `string` | UI icon vs. the texture actually drawn on the pawn/weapon |
| `sortOrder` | `float` | Order within its `decorationType` grouping |
| `appliesTo` / `appliesToAll` | `List<string>` / `bool` | Which `ThingDef`s (by defName) this decoration is offered on |
| `decorationType` | `DecorationTypeDef` | Category grouping shown in the tab (see below) |
| `drawData` | `DrawData` | Default offset/scale/layer per rotation |
| `drawSize` | `Vector2` | Draw size multiplier |
| `colourable` | `bool` | Whether the player can recolour this decoration |
| `colorAmount` | `int` | 1–3 recolourable channels |
| `defaultColour` / `defaultColourTwo` / `defaultColourThree` | `Color?` | Defaults if not colourable-by-player, or the initial picker value if it is |
| `useParentColourAsDefault` | `bool` | Default to the item's own `CompMultiColor` colours instead of white |
| `hasParentColourPaletteOption` | `bool` | Offers "match parent colour" as a picker option |
| `flipable` | `bool` | Can be mirrored; a second click on an already-attached flipable decoration flips it instead of removing it |
| `defaultMask` | `MaskDef` | Mask applied by default |
| `availablePresets` | `List<DecorationColourPresetDef>` | Named colour presets specific to this decoration |
| `isIncompatibleWithBaseTexture` | `bool` | Removed automatically when the item has no alternate base form selected (see [Alternate base textures](Customization-Framework.md#alternate-base-textures)) |
| `incompatibleDecorations` | `List<DecorationDef>` | Mutually exclusive with these other decorations |
| `mustHaveRank` / `mustHaveGene` / `mustHaveTrait` / `mustHaveHediff` | requirement lists | Gate the decoration to pawns meeting them — enforced by `HasRequirements(pawn, out lockedReason)`, shown greyed-out with a tooltip reason otherwise |
| `statOffsets` / `statFactors` | `List<StatModifier>` | Applied to the wearer/weapon while attached (see below) |
| `givesAbilities` / `givesVFEAbilities` | ability lists | Granted while attached, removed when detached |

### Decoration categories

`DecorationTypeDef` (`1.6/Defs/Customization/DecorationTypeDefs.xml`) is just a labelled grouping
bucket — define your own for a new kind of decoration, or reuse a shared one:

`BEWH_DecoCategory_Misc`, `_Decals`, `_Trinkets`, `_Pseals` (purity seals) apply to both;
`_Sights`, `_Wraps` are weapon-only by convention; `_Fabrics`, `_Grenades`, `_Pouches`, `_Halo`,
`_Relics`, `_Shoulder`, `_Wings`, `_Brims`, `_Insignia` are armor-only by convention. There's no
hard restriction enforced in code — these are just the categories the framework ships with.

### Stats and requirements

Every attached decoration's `statOffsets`/`statFactors` are summed/multiplied together by the
owning comp (`CompDecorativeBase.GetStatOffset`/`GetStatFactor`, cached and invalidated on
change) and surface on the item's info card under **Decoration Offsets** / **Decoration Factors**
(`Core40kDefOf.BEWH_DecorationOffsets`/`BEWH_DecorationFactors`). Requirements
(`mustHaveRank`/`mustHaveGene`/`mustHaveTrait`/`mustHaveHediff`) are re-checked whenever the item
is equipped (`RemoveInvalidDecorations`), so a decoration a pawn no longer qualifies for is
stripped automatically rather than left silently attached.

## Armor decorations

`CompDecorative` (`CompProperties_Decorative`) is the comp; `ExtraDecorationDef` is the concrete
decoration def type.

```xml
<ThingDef ParentName="ApparelMakeableBase">
  <defName>BEWH_Apparel_PowerArmor</defName>
  <comps>
    <li Class="Core40k.CompProperties_Decorative">
      <decorativeType>Body</decorativeType>
      <decorations> <!-- decorations attached from the start, no player action needed -->
        <li>BEWH_Deco_PurityScrolls</li>
      </decorations>
    </li>
  </comps>
</ThingDef>

<Core40k.ExtraDecorationDef>
  <defName>BEWH_Deco_ShoulderPad_SkullTrophy</defName>
  <label>skull trophy</label>
  <decorationType>BEWH_DecoCategory_Trinkets</decorationType>
  <iconPath>UI/Decoration/SkullTrophy_Icon</iconPath>
  <drawnTextureIconPath>Things/Decoration/SkullTrophy</drawnTextureIconPath>
  <appliesTo><li>BEWH_Apparel_PowerArmor</li></appliesTo>
  <colourable>true</colourable>
  <colorAmount>2</colorAmount>
  <mustHaveRank>
    <li>BEWH_Rank_Sergeant</li>
  </mustHaveRank>
  <appliesToBodyTypes> <!-- Extra field on ExtraDecorationDef, not the shared DecorationDef -->
    <li>Male</li>
    <li>Hulk</li>
  </appliesToBodyTypes>
</Core40k.ExtraDecorationDef>
```

`ExtraDecorationDef`-only fields: `decorativeType` (comp property: `Body` or `Head`, controlling
which `PawnRenderNode` it attaches to — see `PawnRenderNodeWorker_AttachmentExtraDecorationBody`/
`...Head`), `drawInHeadSpace`, `decoSizeMatchesThingSize`, `defaultShowRotation` (which facings it
renders on by default), and `appliesToBodyTypes` (restricts the decoration to specific
`BodyTypeDef`s — checked against the pawn's body type, accounting for
`DefModExtension_ForcesBodyType` on the worn apparel).

There is also `PawnRenderNodeWorker_AttachmentShoulderPad` and `PawnRenderNodeWorker_AttachmentBackpack`
for the two most common armor decoration slots, and `Dialog_EditExtraDecorationPresets` /
`ExtraDecorationPresetDef` / `DecorationPresetDef` for saving a set of attached decorations as a
reusable named preset (also loadable from XML via `DefModExtension_PawnKindCustomization.extraDecorationPreset`,
see [Customization Framework](Customization-Framework.md#applying-default-looks-per-pawnkindfaction)).

## Weapon decorations

`CompWeaponDecoration` (`CompProperties_WeaponDecoration`) is the comp; `WeaponDecorationDef` is
the concrete decoration def type. It adds two fields beyond the shared base:

| Field | Type | Meaning |
|---|---|---|
| `layerPlacement` | `float` | Draw layer relative to the weapon sprite |
| `weaponSpecificDrawData` | `Dictionary<string, DrawData>` | Per-hostweapon-defName offset/scale/layer override, for decorations that need different placement on different weapon models |
| `verbModifier` | `VerbModifier` | Flat additions to the host weapon's ranged verb — see below |

```xml
<Core40k.WeaponDecorationDef>
  <defName>BEWH_Deco_RedDotSight</defName>
  <label>red dot sight</label>
  <decorationType>BEWH_DecoCategory_Sights</decorationType>
  <iconPath>UI/Decoration/RedDot_Icon</iconPath>
  <drawnTextureIconPath>Things/Decoration/RedDot</drawnTextureIconPath>
  <layerPlacement>2</layerPlacement>
  <verbModifier>
    <additionalWarmupTime>-0.15</additionalWarmupTime>
  </verbModifier>
</Core40k.WeaponDecorationDef>
```

`VerbModifier` (`additionalBurstShotCount`, `additionalRange`, `additionalWarmupTime`) is applied
through postfixes on the weapon's `Verb.BurstShotCount`/`EffectiveRange`/`WarmupTime` getters
(`HarmonyPatch_Increase*FromVarious`) — ranged verbs only; melee tool verbs are explicitly
excluded so a decoration's ranged-only modifier can't leak onto a bash/stab attack.

### Decorations that grant melee tools or ranged verbs

A `WeaponDecorationDef` can also add whole new `Tool`s (melee attacks — a bayonet, a spiked grip)
or `VerbProperties` (an extra ranged verb — an underbarrel launcher) to whatever weapon it's
attached to, and suppress tools/verbs the host weapon already has:

```xml
<Core40k.WeaponDecorationDef>
  <defName>BEWH_Deco_Bayonet</defName>
  <label>bayonet</label>
  <decorationType>BEWH_DecoCategory_Misc</decorationType>
  <iconPath>Things/Decoration/Weapon/Bayonet_Icon</iconPath>
  <drawnTextureIconPath>Things/Decoration/Weapon/Bayonet</drawnTextureIconPath>
  <layerPlacement>2</layerPlacement>

  <!-- Melee attack granted to whatever weapon this is fitted to. Same shape as ThingDef.tools. -->
  <tools>
    <li>
      <label>bayonet</label>
      <capacities><li>Stab</li></capacities>
      <power>14</power>
      <cooldownTime>1.8</cooldownTime>
      <armorPenetration>0.20</armorPenetration>
    </li>
  </tools>

  <!-- Optional: matches Tool.id or Tool.label on the host weapon. Vanilla guns use
       "grip", "barrel" and "stock". -->
  <disablesWeaponTools>
    <li>barrel</li>
  </disablesWeaponTools>
</Core40k.WeaponDecorationDef>
```

```xml
<Core40k.WeaponDecorationDef>
  <defName>BEWH_Deco_UnderbarrelLauncher</defName>
  <label>underbarrel grenade launcher</label>
  <!-- ...graphics as above... -->
  <verbs>
    <li>
      <label>fire grenade</label>
      <verbClass>Verb_LaunchProjectile</verbClass>
      <hasStandardCommand>true</hasStandardCommand> <!-- needed for its own gizmo -->
      <defaultProjectile>Bullet_Frag</defaultProjectile>
      <warmupTime>1.5</warmupTime>
      <range>18</range>
      <soundCast>Mortar_LaunchA</soundCast>
      <targetParams><canTargetLocations>true</canTargetLocations></targetParams>
    </li>
  </verbs>
</Core40k.WeaponDecorationDef>
```

Other fields: `disablesWeaponVerbs` (matches `VerbProperties.label`) and `disablesAllWeaponTools`
(bool). Notes worth knowing before relying on this:

- **Tool ids are namespaced** to `defName + "_" + index` so a decoration's tools can't collide
  with the host weapon's own tool ids.
- **Ordering is by decoration defName**, and decoration verbs always come after the weapon's own
  verbs, so the weapon's own primary verb is never displaced; `isPrimary` is force-set `false` on
  every decoration verb regardless of its XML value.
- **Tools are copied per weapon**, not shared def-level instances — safe for mods (e.g.
  `Comp_ForceWeapon`, see [Comps and Abilities](Comps-and-Abilities.md)) that mutate a weapon's
  own `Tool.extraMeleeDamages` at runtime.
- **Melee DPS / Armor Penetration on the info card do not include decoration tools** — those
  vanilla stat workers read `ThingDef.tools` directly and can't see per-weapon-instance
  decorations. `CompWeaponDecoration.SpecialDisplayStats` instead adds an **"Added melee
  attacks"** entry listing each granted tool's damage/AP/cooldown.
- **Extra ranged verbs are player-driven only** (gizmo / explicit force-attack order) — pawn
  combat AI always picks the weapon's own primary verb, so an underbarrel launcher won't fire
  itself.
- Changing decorations mid-combat rebuilds the weapon's whole verb list, resetting any in-progress
  cooldown/warmup on that weapon. In practice decorations are only changed at a styling station,
  so this isn't user-visible.

## Presets

`DecorationPreset`/`DecorationPresetDef` and the older, apparel-specific
`ExtraDecorationPreset`/`ExtraDecorationPresetDef` both store a named set of decorations (with
their colours/flip state/mask) that can be saved from the customization tab
(`Dialog_EditExtraDecorationPresets`) and re-applied in one click, or assigned as a pawnkind's
default via `DefModExtension_PawnKindCustomization.extraDecorationPreset`.
