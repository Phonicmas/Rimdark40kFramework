# DefModExtension Reference

Small, single-purpose `DefModExtension`s that don't belong to one of the bigger systems
documented elsewhere. Each is driven by a small, targeted Harmony patch — check before writing
your own equivalent.

## On a weapon (`ThingDef.modExtensions`, read from `DamageInfo.Weapon`)

### `DefModExtension_CriticalHit`

```xml
<li Class="Core40k.DefModExtension_CriticalHit">
  <criticalHitChance>0.1</criticalHitChance>          <!-- 0-1 -->
  <criticalHitDamageMultiplier>1.5</criticalHitDamageMultiplier>
</li>
```
Prefixes `Thing.TakeDamage`: each hit from this weapon has `criticalHitChance` to have its damage
amount multiplied before anything else processes it.

### `DefModExtension_BeheadingCut`

```xml
<li Class="Core40k.DefModExtension_BeheadingCut">
  <neckTargetingChance>1</neckTargetingChance> <!-- 0-1 -->
</li>
```
Postfixes both `DamageWorker_Cut.ChooseHitPart` and `DamageWorker_AddInjury.ChooseHitPart`: when
the weapon dealing damage carries this extension, re-rolls the hit location onto the neck with
the given chance (unless it already landed there) — the mechanism behind "this weapon can
behead."

## On an apparel (`ThingDef.modExtensions`)

### `DefModExtension_ExclusiveApparel`

```xml
<li Class="Core40k.DefModExtension_ExclusiveApparel">
  <requiredGene>BEWH_Gene_Astartes</requiredGene>
</li>
```
Put on the **apparel's** `ThingDef` (despite the gene-sounding name, this one lives on the item,
not the gene) — postfixes `EquipmentUtility.CanEquip` so only pawns with the active gene can wear
or wield it, with a translated refusal reason.

## On a gene (`GeneDef.modExtensions`)

### `DefModExtension_GenderDistribution`

```xml
<li Class="Core40k.DefModExtension_GenderDistribution">
  <female>50</female>
  <male>50</male> <!-- the two should sum to 100 -->
</li>
```
Postfixes `PawnGenerator.GenerateGenes`: if the pawn ends up with an active gene carrying this
extension, rerolls `pawn.gender` using the given split — skipped entirely if any active gene on
the pawn carries VEF's own `GeneExtension.forceFemale`/`forceMale` (that takes precedence).

### `DefModExtension_BoostQualityCreatedByPawn`

```xml
<li Class="Core40k.DefModExtension_BoostQualityCreatedByPawn">
  <qualityBoostLevel>
    <Crafting>1</Crafting> <!-- SkillDef -> QualityCategory steps to add -->
  </qualityBoostLevel>
</li>
```
Postfixes `QualityUtility.GenerateQualityCreatedByPawn`: if the pawn has an active gene carrying
this extension whose `qualityBoostLevel` mentions the relevant skill, bumps the rolled
`QualityCategory` up by that many steps (summed across every qualifying gene), capped at Legendary.

### `DefModExtension_GeneExtension`

```xml
<li Class="Core40k.DefModExtension_GeneExtension">
  <addedWorldCarryCapacity>10</addedWorldCarryCapacity>
</li>
```
Flat bonus to world-map caravan carry capacity for pawns with the active gene
(`HarmonyPatch_AddedWorldCarryCapacityPatch`). Not to be confused with VEF's own
`VEF.Genes.GeneExtension` (a different type, from a different mod) referenced above.

### `DefModExtension_InheritableArchite`

```xml
<li Class="Core40k.DefModExtension_InheritableArchite">
  <presentOnBothParentsRequired>true</presentOnBothParentsRequired>
</li>
```
Controls whether this archite gene needs to be present on **both** biological parents to be
inheritable by their child, rather than vanilla's usual one-parent-suffices archite inheritance
(`HarmonyPatch_SpecificInheritableArchiteGenes`).

### `DefModExtension_GeneDisabledBy`

See [Gene System](Gene-System#gene_disabledby) — pairs with `Gene_DisabledBy`.

## On a recipe (`RecipeDef.modExtensions`)

### `DefModExtension_RequiresGene` / `DefModExtension_RequiresHediff`

```xml
<li Class="Core40k.DefModExtension_RequiresGene"><geneDef>BEWH_Gene_Astartes</geneDef></li>
<li Class="Core40k.DefModExtension_RequiresHediff"><hediffDef>BEWH_Hediff_Implanted</hediffDef></li>
```
Consumed by `Recipe_InstallImplantRequiringGene`/`Recipe_InstallImplantRequiringHediff` — see
[Damage and Recipes](Damage-and-Recipes#implant-recipes).

### `DefModExtension_DontPlaceProduct`

```xml
<li Class="Core40k.DefModExtension_DontPlaceProduct" />
```
Marker extension (no fields). Prefixes `GenRecipe.MakeRecipeProducts` to return an empty result
instead of spawning the recipe's normal product — for a recipe whose only purpose is its side
effects (installing a hediff, consuming ingredients) with nothing that should land on the floor.

### `DefModExtension_LockedByResearch`

```xml
<li Class="Core40k.DefModExtension_LockedByResearch">
  <researchs>
    <li>BEWH_Research_ImprovedTechnique</li>
  </researchs>
</li>
```
Prefixes `RecipeDef.AvailableNow`: **retires** this recipe — makes it unavailable — once *any*
listed research is finished. This is the opposite of a normal research lock: it's for a recipe
that should disappear once a better one has been unlocked, not one gated behind research.

## On genes/pawns for slavery, recruitment, and trading

### `DefModExtension_SlaveabilityRecruitability`

```xml
<li Class="Core40k.DefModExtension_SlaveabilityRecruitability">
  <canBeEnslaved>false</canBeEnslaved>
  <canBeRecruited>false</canBeRecruited>
</li>
```
On a `GeneDef`. Prefixes `InteractionWorker_EnslaveAttempt.Interacted`/
`InteractionWorker_RecruitAttempt.Interacted`: blocks the attempt outright (with a translated
letter) for any non-player-faction pawn carrying an active gene that sets the relevant flag false.
Player-faction pawns are never affected (you can't attempt to enslave/recruit your own colonist
anyway).

### `DefModExtension_UntradeablePawn`

```xml
<li Class="Core40k.DefModExtension_UntradeablePawn" />
```
Marker extension, put on a **`XenotypeDef`** (not the pawn/gene). Postfixes
`StockGenerator_Slaves.GenerateThings` to filter any pawn of that xenotype out of slave-trader
stock.

### `DefModExtension_InitialGoodwill`

```xml
<li Class="Core40k.DefModExtension_InitialGoodwill">
  <initialGoodwill>-100</initialGoodwill>
  <applyToPlayer>false</applyToPlayer>
  <onlyApplyToPlayer>false</onlyApplyToPlayer>
</li>
```
On a `FactionDef`. Postfixes `Faction.TryMakeInitialRelationsWith`: forces the starting goodwill
between this faction and whichever other faction it's first meeting to `initialGoodwill`.
`applyToPlayer: false` (the default) skips setting it when one side is the player — set it `true`
to include the player; `onlyApplyToPlayer: true` restricts it to *only* player-facing relations.
If **both** factions in a meeting carry the extension, the instigating faction's copy wins.

### `DefModExtension_LostHeartSurvival` / `DefModExtension_LostLungSurvival`

```xml
<li Class="Core40k.DefModExtension_LostHeartSurvival" />
```
Marker extensions, put on a `GeneDef` that also has a `capMods` entry for `BloodPumping` /
`Breathing` respectively. Postfix `PawnCapacityWorker_BloodPumping`/`_Breathing.CalculateCapacityLevel`:
when vanilla's calculation would otherwise put the capacity at or below zero (i.e. the pawn has no
heart/lungs left), falls back to the gene's own `capMods` offset instead — the mechanism behind
"this gene lets the pawn function without a heart/lungs at all."

## Other framework-wide stats

Two generic `StatDef`s (`1.6/Defs/Stats/Stats_Pawns_General.xml`), not tied to any
`DefModExtension`, meant to be granted like any other stat offset/factor (genes, hediffs, ranks,
decorations):

- **`BEWH_ArtificialPartsAffinityFactor`** — a multiplier on how much artificial body parts affect
  this pawn's capacities (`HarmonyPatch_ArtificialPartsAffinityPatch`).
- **`BEWH_RankLearningFactor`** — a multiplier on how much time-held-as-rank a pawn needs before
  meeting a rank's `daysAs` requirement (lower = learns ranks faster). See
  [Rank System](Rank-System).
