# Comps and Abilities

A grab-bag of reusable `ThingComp`, `CompAbilityEffect`, `HediffComp`, and VEF `Ability` classes.
Each is a `compClass`/`geneClass`/`abilityClass`-style plug-in — drop the XML block on your own
def, no C# needed.

## Ability effects

These go under an `AbilityDef`'s `<comps>` (standard `CompAbilityEffect` extension point) unless
noted otherwise.

### `CompAbilityEffect_AoeHit` (`CompProperties_AbilityAoeHit`)

Deals flat or stat-scaled damage to the ability's target pawn, with optional flecks on the target
and/or the cast location.

```xml
<li Class="Core40k.CompProperties_AbilityAoeHit">
  <damageDef>Bullet</damageDef>
  <damageAmount>10</damageAmount>
  <scaleStat>ShootingAccuracyPawn</scaleStat> <!-- optional: damageAmount *= caster's stat value * scaleFactor -->
  <scaleFactor>1</scaleFactor>
  <fleckDefTarget>Fleck_Something</fleckDefTarget>
  <fleckDefLocation>Fleck_SomethingElse</fleckDefLocation>
</li>
```

### `CompAbilityEffect_GiveHediffAndMentalBreak` (`CompProperties_AbilityGiveHediffAndMental`)

Extends vanilla's own `CompAbilityEffect_GiveHediff` (so all its fields — `hediffDef`, `severity`,
etc. — apply) and additionally force-starts a `MentalStateDef` on the target.

```xml
<li Class="Core40k.CompProperties_AbilityGiveHediffAndMental">
  <hediffDef>BEWH_Hediff_Berserk</hediffDef>
  <mentalStateDef>Berserk</mentalStateDef>
</li>
```

### `CompAbilityEffect_HealAndTend` (`CompProperties_AbilityHealAndTend`)

Heals every injury on a downed/bedridden target pawn by a random amount and tends them at a
quality derived from the caster's `MedicalTendQuality` (plus the target's bed, if any). Requires
the target be `Downed` or `InBed`.

```xml
<li Class="Core40k.CompProperties_AbilityHealAndTend">
  <healAmount>(5,15)</healAmount>
  <maxTendValue>1</maxTendValue>
</li>
```

### `CompAbilityEffect_MustHaveGene` (`CompProperties_MustHaveGene`, C# class `CompAbilityEffect_MustHaveGeneTraitOrHediff.cs`)

A pure validity gate — the ability can't be cast on a target lacking the given active gene. Note
the XML `Class=` you actually use is `Core40k.CompProperties_MustHaveGene` (the source file is
named `...GeneTraitOrHediff` but currently only implements the gene check).

```xml
<li Class="Core40k.CompProperties_MustHaveGene">
  <geneDef>BEWH_Gene_Astartes</geneDef>
</li>
```

### `CompAbilityEffect_ResetRanks` (`CompProperties_ResetRanks`)

Calls `CompRankInfo.ResetRanks` on the target (see
[Rank System](Rank-System#resetting-ranks)). Only castable on a pawn that has `CompRankInfo`,
holds at least one rank, holds a rank in the given category, and whose highest rank in that
category is at or below the allowed demotion tier.

```xml
<li Class="Core40k.CompProperties_ResetRanks">
  <rankCategoryDef>BEWH_RankCategory_Astartes</rankCategoryDef>
  <canDemoteToTierInclusive>2</canDemoteToTierInclusive>
  <ownRankAsTier>false</ownRankAsTier> <!-- true: use the CASTER's own highest rank as the ceiling instead -->
</li>
```

### `CompAbilityEffect_WaveAttack` (`CompProperties_AbilityWaveAttack`)

A cone/line AoE that stuns and/or applies a hediff to every pawn caught in the shape — the effect
behind things like a shockwave or breath weapon. Fields: `range`, `lineWidthEnd` (cone width at
max range), `stunTicks`, `hediffDef` (+ `severity`, `replaceExisting`, `onlyBrain` to force it
onto the brain specifically), `effecterDef` (visual, spawned via a pre-cast action 17 ticks before
impact).

### `Comp_DisableIfApparelCovers` (`CompProperties_DisableIfApparelCovers`)

Disables the ability's gizmo (with an explanatory reason) if the pawn is wearing apparel covering
any of the listed `BodyPartGroupDef`s — e.g. a helmet-dependent ability that shouldn't work with a
helmet-covering hat equipped instead.

```xml
<li Class="Core40k.CompProperties_DisableIfApparelCovers">
  <disabledIfCovered><li>FullHead</li></disabledIfCovered>
</li>
```

### `Comp_DisableInactiveGene` (`CompProperties_DisableInactiveGene`)

Hides the ability's gizmo entirely (not just disables it) unless the pawn has the given gene
active.

```xml
<li Class="Core40k.CompProperties_DisableInactiveGene">
  <geneDef>BEWH_Gene_Astartes</geneDef>
</li>
```

## General `ThingComp`s

### `Comp_Aura` (`CompProperties_Aura`)

Every ~8 in-game hours (`IsHashIntervalTick(500)`), refreshes a self-refreshing hediff on every
player-faction pawn within `range` of the parent thing. Draws its radius as a green field outline
while selected.

```xml
<comps>
  <li Class="Core40k.CompProperties_Aura">
    <givesHediff>BEWH_Hediff_UnderStandardBanner</givesHediff>
    <range>10</range>
    <durationOutsideRange>250</durationOutsideRange> <!-- ticks the hediff lingers once out of range -->
  </li>
</comps>
```

### `Comp_DeteriorateOutsideBuilding` (`CompProperties_DeteriorateOutsideBuilding`)

Marks an item as deteriorating unless it's stored inside one of `antiDeteriorateContainers` (and,
if that container has `CompPowerTrader`, only while powered) — reused from Vanilla Expanded's
ancient-item deterioration pattern.

```xml
<li Class="Core40k.CompProperties_DeteriorateOutsideBuilding">
  <antiDeteriorateContainers><li>BEWH_Building_StasisCase</li></antiDeteriorateContainers>
  <deteriorationRateOutside>2</deteriorationRateOutside>
</li>
```

### `Comp_ForceWeapon` (`CompProperties_ForceWeapon`)

Adds a scaling extra melee damage instance (tagged `"Custom extra damage"`, replacing any previous
instance it added) to every `Tool` on the weapon whose capacities intersect
`capacitiesToApplyOn`, recalculated whenever the wielder's `scalingStat` changes — the pattern
behind a psychically-charged force weapon.

```xml
<li Class="Core40k.CompProperties_ForceWeapon">
  <damageDef>Stun</damageDef>
  <capacitiesToApplyOn><li>Blunt</li></capacitiesToApplyOn>
  <scalingStat>PsychicSensitivity</scalingStat>
  <damageScalingFactor>10</damageScalingFactor>
  <minValueToWork>0.5</minValueToWork>
  <scalesPen>false</scalesPen>
  <flatPen>0.2</flatPen>
</li>
```

### `Comp_GivesAbility` (`CompProperties_GivesAbility`)

Grants a single vanilla `AbilityDef` while the item is equipped, removed on unequip.

```xml
<li Class="Core40k.CompProperties_GivesAbility">
  <ability>BEWH_Ability_SomeItemGranted</ability>
</li>
```

## Hediff comps

### `Hediff_SendLetterAtSeverity` (`HediffCompProperties_SendLetterAtSeverity`)

Sends a letter the first time (or every time, if `onlySendOnce: false`) the hediff's severity
reaches `severitySendAt`.

```xml
<comps>
  <li Class="Core40k.HediffCompProperties_SendLetterAtSeverity">
    <severitySendAt>1</severitySendAt>
    <onlySendOnce>true</onlySendOnce>
    <letter>Rite Complete</letter>
    <message>The ritual has taken hold.</message>
    <letterDef>PositiveEvent</letterDef>
  </li>
</comps>
```

### `Hediff_RemoveMentalStateOnHediffEnd` (`HediffCompProperties_RemoveMentalStateOnHediffEnd`)

Ends the pawn's current mental state when this hediff is removed — optionally restricted to one
specific `MentalStateDef` (`specificMentalState`); leave unset to clear whatever mental state the
pawn happens to be in.

## Map-wide hediff ability

`Ability_MapWideHediff` (a `VEF.Abilities.Ability` subclass, so it's used as an
`<abilityClass>` in a VEF ability def, not a vanilla `AbilityDef`) applies a hediff to every
qualifying pawn on the caster's map on cast.

```xml
<modExtensions>
  <li Class="Core40k.DefModExtension_MapWideHediff">
    <hediffDef>BEWH_Hediff_Inspired</hediffDef>
    <affectPlayerColonists>true</affectPlayerColonists>
    <affectEnemies>false</affectEnemies>
    <affectCaster>true</affectCaster>
  </li>
</modExtensions>
```

The hediff's duration (if it has a `HediffComp_Disappears`) is set from the ability def's own
`durationTime`.
