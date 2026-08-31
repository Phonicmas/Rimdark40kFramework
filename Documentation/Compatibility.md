# Compatibility

## Design principle: soft dependencies only

Every compatibility patch the framework ships resolves the mod it targets **by type name at
runtime**, inside `Core40kMod`'s constructor, and simply does nothing if that type isn't found.
None of them are declared as a `<modDependencies>` entry, and none of them use attribute-driven
`[HarmonyPatch]` discovery (`harmony.PatchAll()` resolves those targets eagerly and would throw if
the target mod were absent). If you add a new compatibility patch to the framework, follow the
same pattern — look the target type up with `AccessTools.TypeByName`, patch conditionally, log a
warning (not an error) if the expected member has moved, and never make the other mod a hard or
soft dependency of the framework. Load order relative to the other mod does not matter either way:
`LoadedModManager.LoadAllActiveMods` loads every mod's assembly before any `Mod` is constructed.

## Required dependencies

- **Harmony** (`brrainz.harmony`) — every patch in the framework runs through one shared
  `Harmony("Core40k.Mod")` instance, applied via `harmony.PatchAll()` in `Core40kMod`'s
  constructor.
- **Vanilla Expanded Framework Core** (`OskarPotocki.VanillaFactionsExpanded.Core`) — used for
  `VEF.Abilities` (the ability system behind `Gene_GiveVEFAbility`,
  `DefModExtension_GivesVEFAbility`, and every `givesVFEAbilities` list across ranks, decorations,
  and alternate forms) and `VEF.Genes` (`Gene_AddRandomGeneAndOrTraitByWeight` checks against
  VEF's own `GeneExtension.forceFemale`/`forceMale`).
- Loads after `SmashPhil.VehicleFramework` (`loadAfter`, not a hard dependency) — no direct code
  reference to it currently, this only affects load order.

## Save Our Ship 2

`SaveOurShip2Compat.Apply(harmony)`. When `SaveOurShip2.AccuracyCalculator` resolves, patches its
`ThisMapEvasionBoost`/`SourceMapAccuracyBoost` property getters to add the best crew member's
`BEWH_ShipEvasionSkillOffset`/`BEWH_ShipGunnerySkillOffset` stat. Details, including exactly how
the added skill levels feed SoS2's own dodge/hit-chance curves, are in
[Voidfaring System](Voidfaring-System.md).

## Female Apparel Variants

`FemaleApparelVariantsCompat.Apply(...)`. FAV ships a Harmony **prefix** on
`PawnRenderNode_Apparel.GraphicsFor` that returns `false` (skip original) whenever it builds its
own graphic — which means vanilla's `ApparelGraphicRecordGetter.TryGetGraphicApparel` (the only
caller of the framework's own apparel-graphic prefixes, `CompMultiColor`/mask/three-colour-shader/
`CompAlternateTexture`/`DefModExtension_ForcesBodyType`) never runs at all on apparel FAV has
opinions about — so multi-colour, masks, and alternate textures would silently stop applying.

The fix is a `Priority.First` prefix on FAV's own prefix: when the apparel is something the
framework owns the graphic for (carries `CompMultiColor`, `CompAlternateTexture`, or
`DefModExtension_ForcesBodyType`, or is non-overhead apparel on a pawn wearing something that
forces a body type), it sets FAV's own `__result = true` and returns `false` — skipping *FAV's*
prefix instead, so vanilla's method (and the framework's own prefixes on it) runs normally.

Female-specific art is not lost in the process: `BodyTypeUtils.BodyTypedPath`/`BodyTypedMaskPath`
independently probe for FAV's own `_<BodyTypeDef>_Female` texture/mask naming convention (checking
all four rotations, a superset of FAV's own `_south`-only lookup) whenever the wearer is female,
falling back to the ordinary body-typed path if no such file exists — so this works whether or not
FAV is even installed, and modders supplying art only need to follow FAV's naming convention, not
add anything framework-specific.

## Dual Wield

`DualWieldCompat.Apply(...)`. An off-hand weapon (in the Dual Wield mod) never goes through
vanilla's `Verb.TryStartCastOn` — Dual Wield drives it through its own copy,
`DualWield.Ext_Verb.OffhandTryStartCastOn`, which reads `Verb.verbProps.warmupTime` as a **raw
field access** rather than going through the `Verb.WarmupTime` property. That skips every
postfix on the property, including `DefModExtension_AmmoChanger.warmupTime` (see
[Changeable Ammo](Changeable-Ammo.md)) and a weapon decoration's `verbModifier.additionalWarmupTime`
(see [Decorations](Decorations.md#weapon-decorations)) — a precision firing mode or a scope
decoration would fire at the weapon's unmodified base speed while held off-hand.
