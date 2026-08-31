# RimDark 40k — Framework

**RimDark 40k - Framework** (`Phonicmas.RimDark.FrameworkCore`) is the shared code and systems
layer for the RimDark 40k mod family. It does not add any content on its own — no pawns,
weapons, or factions. It exists so that RimDark 40k, RimDark 40k - Abhuman, and any other add-on
mod can share one implementation of ranks, apparel/weapon customization, ammo switching,
voidfaring bonuses, and a large set of small reusable `DefModExtension`s and comps, instead of
each add-on reinventing them.

This documentation describes what the framework provides and how a submod (or a standalone mod
that wants to depend on the framework) uses each system. It is written from the current `1.6`
source. `1.5` is still supported by the mod for players, but is not receiving new features —
see [Getting Started](Getting-Started#15-vs-16) for what that means for you.

## How this documentation is organized

| Page | Covers |
|---|---|
| [Getting Started](Getting-Started) | Depending on the framework, project layout, `Core40kDefOf`, versioning |
| [Customization Framework](Customization-Framework) | The styling-station dialog, tabs, multi-colour system, alternate base textures, mask defs, presets |
| [Decorations](Decorations) | Attachable armor & weapon decorations, including decorations that grant melee tools/verbs |
| [Rank System](Rank-System) | `RankDef`/`RankCategoryDef`, requirements, colony limits, pawnkind pre-unlocked ranks, eligibility notifications |
| [Changeable Ammo](Changeable-Ammo) | `Comp_AmmoChanger` — swappable firing modes/ammo with their own stats |
| [Voidfaring System](Voidfaring-System) | Gravship/ship crew stats (Odyssey gravships and Save Our Ship 2) |
| [Gene System](Gene-System) | The framework's reusable `Gene` classes |
| [Comps and Abilities](Comps-and-Abilities) | Reusable `ThingComp`, `CompAbilityEffect`, and `HediffComp` classes |
| [DefModExtension Reference](DefModExtension-Reference) | The smaller, single-purpose `DefModExtension`s not covered on another page |
| [Damage and Recipes](Damage-and-Recipes) | Custom `DamageWorker`s and surgery `Recipe` classes |
| [Mod Settings](Mod-Settings) | What `Core40kModSettings` exposes and where |
| [Compatibility](Compatibility) | Built-in soft compatibility with Save Our Ship 2, Female Apparel Variants, Dual Wield, and framework dependencies |

## What the framework provides, at a glance

- **A rank system** — configurable rank trees with skill/gene/trait/hediff/royal-title
  requirements, colony population limits, per-rank stat/hediff/ability grants, and a dedicated
  pawn inspect tab.
- **A styling-station customization framework** — a shared dialog + tab system that any apparel
  or weapon can opt into, backing three concrete features: multi-colour (2–3 colour) recolouring,
  swappable alternate base textures, and attachable decorations.
- **A decoration system** — small attachable pieces (trinkets, purity seals, sights, bayonets,
  underbarrel launchers, etc.) with their own colours, stat modifiers, requirements, and — for
  weapons — the ability to grant extra melee tools or ranged verbs.
- **A changeable-ammo system** — a gizmo that swaps a weapon's projectile/firing mode, with each
  mode able to override burst count, range, warmup time, and any other weapon stat.
- **Voidfaring bonuses** — pawn stats that boost Odyssey gravships and (optionally) Save Our
  Ship 2 vessels based on the best crew member aboard.
- **A grab-bag of small, generic `DefModExtension`s and comps** used throughout RimDark 40k content
  mods — weighted random gene/trait grants, exclusive apparel, critical hits, holy/warp damage,
  disappearing hediffs that send a letter, and more.

## Dependencies

- **Harmony** (`brrainz.harmony`)
- **Vanilla Expanded Framework Core** (`OskarPotocki.VanillaFactionsExpanded.Core`) — the
  framework uses VEF's ability system (`VEF.Abilities`) for several of its gene/rank/decoration
  ability grants.
- Loads after `Ludeon.RimWorld`, `brrainz.harmony`, `OskarPotocki.VanillaFactionsExpanded.Core`,
  and `SmashPhil.VehicleFramework`.

## Migrating this folder into the GitHub Wiki

These pages are written to drop directly into a GitHub wiki with no changes: every internal link
points at another page's name without the `.md` extension (`Rank-System`, etc.). GitHub wiki
renders each page at a URL named after its file (`Page-Name.md` becomes `/wiki/Page-Name`), and
only resolves links in that extension-less form to the rendered page — a link that still has
`.md` on the end opens the raw file content instead.

```
git clone https://github.com/Phonicmas/Rimdark40kFramework.wiki.git
cp Documentation/*.md Rimdark40kFramework.wiki/
cd Rimdark40kFramework.wiki
git add .
git commit -m "Import framework documentation"
git push
```

`_Sidebar.md` is a GitHub wiki special page — once pushed, it renders automatically as the wiki's
sidebar navigation on every page.
