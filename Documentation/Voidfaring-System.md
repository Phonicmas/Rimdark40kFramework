# Voidfaring System

A small set of pawn `StatDef`s under the `BEWH_Voidfaring` stat category that boost a spacefaring
vessel based on the **best** crew member standing aboard it — these do not stack between crew.
Each stat is defined unconditionally (so submods and saves are stable whether or not the relevant
expansion/mod is installed) but only does anything when its target system is present, and each
is `hideAtValue`d so a pawn nobody ever granted it doesn't show a meaningless "0%" line on their
stat card.

## The stats

| StatDef | Applies to | Effect |
|---|---|---|
| `BEWH_GravshipFuelEfficiency` | Odyssey gravships | Added to the gravship's own `FuelSavingsPercent`, capped at 90% total savings |
| `BEWH_GravEngineCooldownFactor` | Odyssey gravships | Multiplies the grav engine's post-landing recharge time (lower = faster), applied on top of the cooldown the launch ritual's quality already earned |
| `BEWH_GravshipRangeOffset` | Odyssey gravships | Flat extra tiles added to `GravshipRange` |
| `BEWH_ShipEvasionSkillOffset` | Save Our Ship 2 | Effective piloting-skill levels added to a ship's dodge chance (diminishing past ~20, nothing past 22) |
| `BEWH_ShipGunnerySkillOffset` | Save Our Ship 2 | Effective gunnery-skill levels added to a ship's chance to land hits (nothing past ~20) |

Granting one of these to a pawn — via a gene, hediff, rank (`RankDef.statOffsets`), decoration,
or ordinary `StatPart`/ability effect — is all you need to do; the framework's own patches read
them automatically. There is nothing to opt a *ship* into; every gravship/SoS2 vessel is affected
by whichever crew member aboard it scores highest.