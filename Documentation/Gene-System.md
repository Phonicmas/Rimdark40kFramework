# Gene System

Four reusable `Gene` classes for XenotypeDef/GeneDef-driven random or conditional effects. Each
pairs with a `DefModExtension` that supplies its configuration, so the gene class itself never
needs subclassing per use.

## `Gene_AddRandomGeneAndOrTraitByWeight`

Rolls a weighted-random gene and/or trait once, when the gene is added (`PostMake`/`PostAdd`),
and removes exactly what it granted when the gene is removed. The two halves
(gene grant, trait grant) are independent — a `GeneDef` can carry either extension, both, or
neither.

```xml
<GeneDef>
  <defName>BEWH_Gene_VeteranLineage</defName>
  <!-- ... -->
  <geneClass>Core40k.Gene_AddRandomGeneAndOrTraitByWeight</geneClass>
  <modExtensions>
    <li Class="Core40k.DefModExtension_AddRandomGeneByWeight">
      <possibleGenesToGive>
        <BEWH_Gene_KeenSenses>2</BEWH_Gene_KeenSenses>
        <BEWH_Gene_ThickSkin>1</BEWH_Gene_ThickSkin>
      </possibleGenesToGive>
      <amountToGive>1</amountToGive>            <!-- IntRange; also accepts a fixed int like "2" -->
      <chanceToGrantGene>75</chanceToGrantGene>  <!-- 0-100 -->
      <skipIfAnyAlreadyExistsOnPawn>false</skipIfAnyAlreadyExistsOnPawn>
      <addAsXenogene>true</addAsXenogene>
    </li>
    <li Class="Core40k.DefModExtension_AddRandomTraitByWeight">
      <possibleTraitsToGive>
        <li><traitDef>Tough</traitDef><weight>1</weight></li>
        <li><traitDef>Bloodlust</traitDef><degree>0</degree><weight>0.5</weight></li>
      </possibleTraitsToGive>
      <amountToGive>1</amountToGive>
      <chanceToGrantTrait>100</chanceToGrantTrait>
    </li>
  </modExtensions>
</GeneDef>
```

`amountToGive` (int, on the trait side) has three modes: `1` picks one weighted entry;
equal to the full list's count grants every listed entry; anything in between draws that many
*unique* entries weighted-random. `skipIfAnyAlreadyExistsOnPawn` (gene side only) skips the whole
roll if the pawn already has fewer eligible candidates than the full list (i.e. already carries at
least one of the listed genes).

## `Gene_ChanceToAddEachGene`

Independently rolls each candidate gene on its own percentage — unlike the weighted picker above,
more than one (or none, or all) can be granted from the same list.

```xml
<modExtensions>
  <li Class="Core40k.DefModExtension_ChanceToAddEachGene">
    <possibleGenesToGive>
      <BEWH_Gene_KeenSenses>30</BEWH_Gene_KeenSenses>   <!-- each value is an independent 0-100 chance -->
      <BEWH_Gene_ThickSkin>15</BEWH_Gene_ThickSkin>
    </possibleGenesToGive>
  </li>
</modExtensions>
```

## `Gene_DisabledBy`

Overrides `Gene.Active` to return `false` while the pawn has any gene listed in
`DefModExtension_GeneDisabledBy.geneDisabledBy` active — useful for mutually-exclusive gene lines
where the "better" gene should silently suppress a weaker one rather than being incompatible
outright.

```xml
<GeneDef>
  <defName>BEWH_Gene_BasicResilience</defName>
  <geneClass>Core40k.Gene_DisabledBy</geneClass>
  <modExtensions>
    <li Class="Core40k.DefModExtension_GeneDisabledBy">
      <geneDisabledBy><li>BEWH_Gene_AdvancedResilience</li></geneDisabledBy>
    </li>
  </modExtensions>
</GeneDef>
```

## `Gene_GiveVEFAbility`

Grants (and cleanly revokes) one or more Vanilla Expanded Framework abilities
(`VEF.Abilities.AbilityDef`, via `CompAbilities`) for as long as the gene is active — this is for
VEF's own ability system, distinct from vanilla `AbilityDef`/`Pawn_AbilityTracker` which every
other `givesAbilities` list in this framework (ranks, decorations, alternate forms) also supports
side-by-side.

```xml
<GeneDef>
  <defName>BEWH_Gene_PsykerPotential</defName>
  <geneClass>Core40k.Gene_GiveVEFAbility</geneClass>
  <modExtensions>
    <li Class="Core40k.DefModExtension_GivesVEFAbility">
      <abilityDefs>
        <li>VEF_Ability_Something</li>
      </abilityDefs>
    </li>
  </modExtensions>
</GeneDef>
```
