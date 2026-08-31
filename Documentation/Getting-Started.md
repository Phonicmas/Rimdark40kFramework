# Getting Started

## Depending on the framework

Add the framework as a mod dependency in your `About/About.xml`:

```xml
<modDependencies>
  <li>
    <packageId>Phonicmas.RimDark.FrameworkCore</packageId>
    <displayName>RimDark 40k - Framework</displayName>
  </li>
</modDependencies>
<loadAfter>
  <li>Phonicmas.RimDark.FrameworkCore</li>
</loadAfter>
```

Everything the framework defines lives in the `Core40k` C# namespace and is compiled into
`Core40k.dll`. Reference that assembly from your submod's `.csproj` the same way the framework's
own project references `Assembly-CSharp.dll` — a `<HintPath>` to the built
`Core40k.dll` under `Rimdark40kFramework/1.6/Assemblies/`, `<Private>False</Private>` (it must not
be copied into your own mod's `Assemblies` folder — RimWorld already loads it from the framework).


## Where to go next

- Adding coloring/decoration support to a new piece of apparel or a weapon:
  [Customization Framework](Customization-Framework) and [Decorations](Decorations).
- Giving a pawnkind a rank tree, or adding ranks other content can require:
  [Rank System](Rank-System).
- Giving a weapon swappable ammo/firing modes: [Changeable Ammo](Changeable-Ammo).
- Looking for a small utility extension (exclusive apparel, critical hits, weighted random
  gene/trait, etc.) before writing your own: [DefModExtension Reference](DefModExtension-Reference)
  and [Comps and Abilities](Comps-and-Abilities).
