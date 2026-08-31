# Rank System

The rank system gives pawns a comp-backed, requirement-gated progression of `RankDef`s, grouped
into `RankCategoryDef` trees, with its own pawn inspect tab. It's used for things like a Space
Marine's chapter rank ladder, but is entirely generic — any pawn can carry it.

## Adding the comp

```xml
<ThingDef ParentName="BasePawn">
  <defName>BEWH_SpaceMarine</defName>
  <comps>
    <li Class="Core40k.CompProperties_RankInfo" />
  </comps>
</ThingDef>
```

`CompRankInfo` (`ThingComp`) is the runtime state: which `RankDef`s a pawn holds, how many days
they've held each, and which limited-rank slots they occupy. The rank tab (`ITab_RankSystem`,
translation key `BEWH.Framework.RankSystem.RankTab`) only appears for pawns carrying this comp
(or always, if `Core40kModSettings.alwaysShowRankTab` is enabled in dev/debug settings).

## `RankCategoryDef` — a rank tree

```xml
<Core40k.RankCategoryDef>
  <defName>BEWH_RankCategory_Astartes</defName>
  <label>Astartes chapter ranks</label>

  <!-- optional: gate the whole tree behind a gene/hediff/trait -->
  <unlockedByGene>BEWH_Gene_Astartes</unlockedByGene>
  <!-- lockedByGene / lockedByHediff / lockedByTrait (+ traitDegree) also available -->

  <!-- optional (Royalty only, see below) -->
  <unlockedByTitle MayRequire="Ludeon.RimWorld.Royalty">Knight</unlockedByTitle>
  <lockedByTitle MayRequire="Ludeon.RimWorld.Royalty">Baron</lockedByTitle>

  <ranks>
    <li>
      <rankDef>BEWH_Rank_Neophyte</rankDef>
      <displayPosition>(0,0)</displayPosition> <!-- position in the tab's tree layout -->
    </li>
    <li>
      <rankDef>BEWH_Rank_Battlebrother</rankDef>
      <displayPosition>(0,1)</displayPosition>
      <rankRequirements>
        <BEWH_Rank_Neophyte>30</BEWH_Rank_Neophyte> <!-- must have held this rank >= 30 days -->
      </rankRequirements>
    </li>
    <li>
      <rankDef>BEWH_Rank_Sergeant</rankDef>
      <displayPosition>(0,2)</displayPosition>
      <rankRequirementsOneAmong>
        <BEWH_Rank_Battlebrother>60</BEWH_Rank_Battlebrother>
        <BEWH_Rank_Apothecary>0</BEWH_Rank_Apothecary>
      </rankRequirementsOneAmong>
    </li>
  </ranks>
</Core40k.RankCategoryDef>
```

Each `<li>` under `ranks` is a `RankCategorySpecificData`: the `rankDef`, its `displayPosition`
in the tab's tree layout, and its prerequisites within *this* category —
`rankRequirements` (must hold **all** listed ranks for at least the given number of days) and
`rankRequirementsOneAmong` (must hold **at least one**). A rank can appear in more than one
category, and its prerequisites are specific to whichever category it's being evaluated in.

## `RankDef` — one rank

```xml
<Core40k.RankDef>
  <defName>BEWH_Rank_Sergeant</defName>
  <label>sergeant</label>
  <rankTier>3</rankTier>                 <!-- ordering; UnlockRank sets pawn.story.Title from the highest tier held -->
  <rankIconPath>UI/Rank/Sergeant</rankIconPath>
  <newPawnCardTitle>Sergeant</newPawnCardTitle> <!-- defaults to label if unset -->

  <colonyLimitOfRank>(1, 0)</colonyLimitOfRank> <!-- x: base slots, y: +1 slot per y colonists. See below -->

  <requiredSkills>
    <li><skill>Melee</skill><level>8</level></li>
  </requiredSkills>
  <requiredGenesAll><li>BEWH_Gene_Astartes</li></requiredGenesAll>
  <requiredTraitsOneAmong>
    <li><traitDef>Bloodlust</traitDef><degree>0</degree></li>
  </requiredTraitsOneAmong>
  <incompatibleRanks><li>BEWH_Rank_Chaplain</li></incompatibleRanks>

  <!-- Royalty-only, see "Title requirements" below -->
  <requiredTitlesOneAmong>
    <li MayRequire="Ludeon.RimWorld.Royalty">Knight</li>
  </requiredTitlesOneAmong>

  <!-- What unlocking the rank grants -->
  <statOffsets><MeleeHitChance>0.05</MeleeHitChance></statOffsets>
  <statFactors><MoveSpeed>1.1</MoveSpeed></statFactors>
  <givesAbilities><li>BEWH_Ability_RallyCry</li></givesAbilities>
  <givesHediffs>
    <li><hediffDef>BEWH_Hediff_VeteranScars</hediffDef><initialSeverity>1</initialSeverity></li>
  </givesHediffs>
  <recreationFromSkills><li>Melee</li></recreationFromSkills> <!-- training counts as recreation -->
  <givesPassions>
    <li><skill>Melee</skill><type>AddOneLevel</type></li> <!-- or DropAll -->
  </givesPassions>
  <removeRanksOnUnlock><li>BEWH_Rank_Neophyte</li></removeRanksOnUnlock>
  <customEffectDescriptions>
    <li>Commands squad formations.</li> <!-- flavour text only, shown in the rank's bonus list -->
  </customEffectDescriptions>
  <specialistRank>false</specialistRank>
  <defaultFirstRank>false</defaultFirstRank>
</Core40k.RankDef>
```

### Colony limits

`colonyLimitOfRank` is a `Vector2`: `x` is a flat cap, `y` is "one more slot per `y` colonists".
`(1, 0)` = exactly 1 in the colony ever; `(0, 10)` = uncapped base, +1 slot per 10 colonists;
`(1, 10)` = 1 base slot, +1 more per 10 colonists. `GameComponent_RankInfo` tracks how many
limited-rank slots are currently taken; `CompRankInfo` only counts a rank against the limit if it
was actually granted through a path that consumes a slot (recruiting, arresting, resurrection,
generating directly into the player faction) — a raider or visitor's rank does not eat a colony
slot until/unless they join the player.

### Title requirements (Royalty)

`requiredTitlesAll`/`requiredTitlesOneAmong` on a `RankDef`, or `unlockedByTitle`/`lockedByTitle`
on a `RankCategoryDef`, take a `RoyalTitleRequirement`:

```xml
<requiredTitlesAll>
  <li MayRequire="Ludeon.RimWorld.Royalty">
    <title>Knight</title>
    <faction>Empire</faction>     <!-- optional: restrict to one faction's title ladder -->
    <exactTitle>false</exactTitle> <!-- false (default): this title or more senior; true: exact match -->
    <inEffectOnly>true</inEffectOnly> <!-- default: ignore titles suspended by faction relations -->
  </li>
</requiredTitlesAll>
```

The shorthand `<li>Knight</li>` form works too (title only, all other fields default).
**When Royalty is not active, title requirements are ignored entirely** rather than making the
rank unreachable — always wrap them in `MayRequire="Ludeon.RimWorld.Royalty"` so they resolve
quietly either way. `exactTitle="false"` compares `RoyalTitleDef.seniority` numerically, so a
modded faction's own title ladder can accidentally satisfy a requirement written against a vanilla
faction's titles unless you set `<faction>`.

## Pre-unlocked ranks on a `PawnKindDef`

`DefModExtension_PawnKindRanks` grants ranks automatically when a pawn of that kind generates
(hooked on `PawnGenerator.GeneratePawn`, after genes/traits/hediffs/gear are already applied, so
gene/hediff/trait-gated categories evaluate correctly):

```xml
<PawnKindDef ParentName="BEWH_SpaceMarineBase">
  <defName>BEWH_UltramarinesSergeant</defName>
  <modExtensions>
    <li Class="Core40k.DefModExtension_PawnKindRanks">
      <ranks>
        <li>
          <rank>BEWH_Rank_Sergeant</rank>
          <daysAsRank>15</daysAsRank> <!-- backdated, so time-gated follow-ups are reachable at once -->
        </li>
        <li> <!-- weighted random pick instead of a fixed rank -->
          <chance>0.25</chance>
          <rankOptions>
            <li><rank>BEWH_Rank_Apothecary</rank><weight>1</weight></li>
            <li><rank>BEWH_Rank_Techmarine</rank><weight>1</weight></li>
            <li><rank>BEWH_Rank_Chaplain</rank><weight>0.5</weight></li>
          </rankOptions>
          <daysAsRank>10</daysAsRank>
        </li>
      </ranks>
    </li>
  </modExtensions>
</PawnKindDef>
```

Each entry: `rank` (fixed) or `rankOptions` (weighted random, used when `rank` is unset),
`rankCategory` (auto-detected from the first category containing the rank if left unset),
`includePrerequisites` (default `true` — also grants everything the rank requires, recursively;
for a `rankRequirementsOneAmong` prerequisite the lowest-tier option is picked), `chance`
(0–1 roll), `daysAsRank`, and `requireCategoryUnlocked` (default `true` — skip the entry if the
pawn fails the category's own unlock gate). Ranks are granted lowest-tier first, so
`pawn.story.Title` ends up reflecting the highest tier actually granted. Only player-faction
pawns generated this way consume colony rank-limit slots.

## Resetting ranks

`CompRankInfo.ResetRanks(RankCategoryDef categoryOrNull)` strips every rank in one category (or,
passed `null`, every rank the pawn holds) and re-arms that category's eligibility-notification
flags so the pawn can be told about them again as they re-earn them. This is exposed to XML as an
ability effect — see `CompAbilityEffect_ResetRanks` in
[Comps and Abilities](Comps-and-Abilities.md#ability-effects).
