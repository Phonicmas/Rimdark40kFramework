# Changeable Ammo

`Comp_AmmoChanger` (`CompProperties_AmmoChanger`) gives a ranged weapon a gizmo that swaps which
projectile/ammo `ThingDef` it fires — and, because each projectile choice can carry its own
`DefModExtension_AmmoChanger`, effectively swaps the weapon's firing mode: burst count, warmup
time, range, and any generic stat offset/factor (accuracy, cooldown, whatever you like).

## Setting it up

```xml
<ThingDef ParentName="BaseGun">
  <defName>BEWH_Weapon_BoltRifle</defName>
  <comps>
    <li Class="Core40k.CompProperties_AmmoChanger">
      <availableProjectiles>
        <li>BEWH_Bullet_Bolt_Standard</li>
        <li>BEWH_Bullet_Bolt_Precision</li>
        <li>BEWH_Bullet_Bolt_Burst</li>
      </availableProjectiles>
    </li>
  </comps>
</ThingDef>
```

Each entry in `availableProjectiles` is an ordinary projectile `ThingDef` — no special base class
needed — optionally carrying a `DefModExtension_AmmoChanger`:

```xml
<ThingDef ParentName="BaseBullet">
  <defName>BEWH_Bullet_Bolt_Precision</defName>
  <label>bolt (precision)</label>
  <projectile>
    <damageDef>Bullet</damageDef>
    <damageAmountBase>18</damageAmountBase>
    <speed>70</speed>
  </projectile>
  <modExtensions>
    <li Class="Core40k.DefModExtension_AmmoChanger">
      <unlockedBy>BEWH_Research_PrecisionRounds</unlockedBy> <!-- optional research gate -->
      <shotsPerBurst>1</shotsPerBurst>
      <warmupTime>1.6</warmupTime>
      <statFactors>
        <AccuracyTouch>1.05</AccuracyTouch>
        <AccuracyShort>1.10</AccuracyShort>
        <AccuracyMedium>1.20</AccuracyMedium>
        <AccuracyLong>1.35</AccuracyLong>
      </statFactors>
    </li>
  </modExtensions>
</ThingDef>

<ThingDef ParentName="BaseBullet">
  <defName>BEWH_Bullet_Bolt_Burst</defName>
  <label>bolt (burst)</label>
  <projectile>
    <damageDef>Bullet</damageDef>
    <damageAmountBase>14</damageAmountBase>
    <speed>70</speed>
  </projectile>
  <modExtensions>
    <li Class="Core40k.DefModExtension_AmmoChanger">
      <shotsPerBurst>5</shotsPerBurst>
      <warmupTime>0.9</warmupTime>
      <statFactors>
        <AccuracyTouch>0.95</AccuracyTouch>
        <AccuracyShort>0.85</AccuracyShort>
        <AccuracyMedium>0.70</AccuracyMedium>
        <AccuracyLong>0.55</AccuracyLong>
      </statFactors>
    </li>
  </modExtensions>
</ThingDef>
```

## Fields

`DefModExtension_AmmoChanger`:

| Field | Type | Meaning |
|---|---|---|
| `unlockedBy` | `ResearchProjectDef` | Ammo without a matching finished research is skipped when picking a fallback default, and blocks its float-menu option — leave unset for no research gate |
| `effectiveRange` | `float?` | Overrides the weapon's `range` while this ammo is loaded |
| `warmupTime` | `float?` | Overrides the weapon's `warmupTime` |
| `shotsPerBurst` | `int?` | Overrides the weapon's `burstShotCount` |
| `statOffsets` / `statFactors` | `List<StatModifier>` | Applied to **the weapon's own stats** (any `StatDef`, not just accuracy) while this ammo is loaded |

`statOffsets`/`statFactors` compose with everything else affecting the weapon's stats (quality,
decorations, alternate-texture stat sources, other mods) rather than overwriting it, because
they're implemented as an ordinary `ThingComp.GetStatOffset`/`GetStatFactor` override — the same
mechanism as [decorations](Decorations#stats-and-requirements). `effectiveRange`/`warmupTime`/
`shotsPerBurst`, by contrast, replace the weapon's own verb values outright once selected (via
patches on `CompEquippable`'s ranged verb getters — melee tool verbs on the same weapon are left
untouched).

`AdjustedAccuracy` clamps each distance band to `[0.01, 1]`, so a `statFactor` above 1 does
nothing on a weapon already at 100% in that band — if a "precision" mode is meant to noticeably
beat the cap, lower the base weapon's own accuracy stats and let the ammo factor bring them back
up.

## How switching works

The gizmo (`Gizmo_AmmoChanger`) opens a float menu of `AvailableProjectiles`; picking one queues
`Core40kDefOf.BEWH_ChangeAmmo`, a short reload job (`JobDriver_ChangeAmmo`, its duration scaled by
the pawn's Manipulation) that calls `Comp_AmmoChanger.LoadNextProjectile()` on completion — the
switch isn't instant. `Comp_AmmoChanger.CurrentlySelectedProjectile` is what actually fires
(`HarmonyPatch_ChangeCurrentProjectile` postfixes `Verb_LaunchProjectile.Projectile`), and falls
back to the first available ammo the pawn has research for, or the weapon's own
`defaultProjectile` if none qualify or the comp hasn't been touched yet.

## Compatibility note

If your weapon can also be dual-wielded (via the Dual Wield mod), see
[Compatibility](Compatibility#dual-wield) — the framework already patches around a Dual Wield
limitation so an off-hand weapon's ammo-changer warmup time applies correctly, but it's worth
knowing that patch exists rather than re-solving the same problem.
