# Damage and Recipes

## Damage workers

### `DamageWorker_Holy`

Pairs with `DefModExtension_HolyDamageExtension` on the `DamageDef`. Split behaviour based on the
victim's relationship to the player:

- **Hostile to the player:** applies the damage `Random(minHitAmount, maxHitAmount)` times in a
  row (each application capped at 999 armor penetration — holy damage ignores armor), then has
  `chanceToIgnite`% to set them alight.
- **Not hostile (ally/neutral/player pawn):** instead of dealing damage, **heals** them —
  `damage amount * healPercentOfDamageToAllies`, split evenly across their current injuries.

```xml
<DamageDef>
  <defName>BEWH_HolyFire</defName>
  <workerClass>Core40k.DamageWorker_Holy</workerClass>
  <!-- ... -->
  <modExtensions>
    <li Class="Core40k.DefModExtension_HolyDamageExtension">
      <chanceToIgnite>20</chanceToIgnite>
      <minHitAmount>2</minHitAmount>
      <maxHitAmount>5</maxHitAmount>
      <healPercentOfDamageToAllies>0.5</healPercentOfDamageToAllies>
    </li>
  </modExtensions>
</DamageDef>
```

Use this for a weapon/explosion that's explicitly meant to smite enemies while being safe (or
beneficial) to stand near for the player's own pawns and allies — a blessed flamer, an emperor's
wrath effect, and similar.

### `DamageWorker_WarpFlame`

`workerClass` for `Core40kDefOf.BEWH_WarpFlame` (a `DamageDef`). Behaves like vanilla flame
damage plus extras: applies the primary hit, then 1–2 additional applications of the
`BEWH_WarpFlame` damage def itself with a chance to attach fire each time; briefly forces the
game off fast-forward when it hits a player-faction pawn (so the player doesn't miss what just
happened); and on destroying its target, scatters ash filth over the occupied cells and turns a
destroyed plant into a burnt stump. If you want your own "warp-touched fire" effect that behaves
like this but with a different `DamageDef`, subclass `DamageWorker_WarpFlame` rather than reusing
the def directly (`BEWH_WarpFlame` is a specific def referenced by name from `Core40kDefOf`).

## Implant recipes

### `Recipe_InstallImplantRequiringGene` / `Recipe_InstallImplantRequiringHediff`

Both extend vanilla's `Recipe_InstallImplant` and only add an `AvailableOnNow` gate: the recipe
is unavailable unless the patient has the active gene / hediff named by
`DefModExtension_RequiresGene`/`DefModExtension_RequiresHediff` (see
[DefModExtension Reference](DefModExtension-Reference#on-a-recipe-recipedefmodextensions)).

```xml
<RecipeDef>
  <defName>BEWH_Recipe_InstallBionicArm_AstartesOnly</defName>
  <workerClass>Core40k.Recipe_InstallImplantRequiringGene</workerClass>
  <modExtensions>
    <li Class="Core40k.DefModExtension_RequiresGene"><geneDef>BEWH_Gene_Astartes</geneDef></li>
  </modExtensions>
  <!-- ...addsHediff, ingredients, etc. as any normal implant recipe... -->
</RecipeDef>
```

### `Recipe_InstallImplantWithLevels`

A surgery recipe (`Recipe_Surgery`) for a hediff meant to be installed *repeatedly*, stacking
severity instead of failing once already present — the mechanism behind something like
"implant level 1 / 2 / 3" progressions:

- If the pawn doesn't have `recipe.addsHediff` yet, adds it normally.
- If they already have it, increments its `Severity` by `1` instead of adding a duplicate.
- `AvailableOnNow` allows the operation again as long as the existing hediff's severity hasn't
  reached `hediffDef.maxSeverity`.
- `GetPartsToApplyOn` restricts candidate body parts to ones without another directly-added part
  already occupying them (via `MedicalRecipesUtility.GetFixedPartsToApplyOn`).

```xml
<RecipeDef>
  <defName>BEWH_Recipe_UpgradeCyberSkull</defName>
  <workerClass>Core40k.Recipe_InstallImplantWithLevels</workerClass>
  <addsHediff>BEWH_Hediff_CyberSkullAugment</addsHediff>
  <!-- run this recipe again later to bump the hediff's severity by 1, up to its maxSeverity -->
</RecipeDef>
```
