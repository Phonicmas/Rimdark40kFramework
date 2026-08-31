# Mod Settings

`Core40kModSettings` (`ModSettings`), exposed through three tabs in the framework's own mod
settings window (`Core40kMod` / `CoreMod`, under **Mod Options → RimDark 40k - Framework
<version>**).

## Main tab (`ModSettingTab_CoreMain`)

| Setting | Field | Default | Effect |
|---|---|---|---|
| Confirm rank unlock | `confirmRankUnlock` | `false` | Adds a confirmation dialog before manually unlocking a rank from the rank tab |
| Notify on rank eligibility | `notifyOnRankEligibility` | `true` | Toggles the [rank eligibility messages](Rank-System.md#eligibility-notifications) |

## Customization tab (`ModSettingTab_CoreCustomization`)

| Setting | Field | Default | Effect |
|---|---|---|---|
| Show customization debug options | `showCustomizationDebugOptions` | `false` | Surfaces extra debug controls in the styling dialogs |
| Decorations per row | `decorationsPerRow` | `6` | Grid width (3–8) for the decoration picker in [decoration tabs](Decorations.md) |

## Debug tab (`ModSettingTab_CoreDebug`)

| Setting | Field | Default | Effect |
|---|---|---|---|
| Always show rank tab | `alwaysShowRankTab` | `false` | Shows the rank inspect tab on every pawn, even those without `CompRankInfo` |

## Reading settings from your own code

```csharp
var settings = LoadedModManager.GetMod<Core40kMod>().GetSettings<Core40kModSettings>();
if (settings.notifyOnRankEligibility) { /* ... */ }
```
`Core40kUtils.ModSettings` is a cached shortcut for the same call, already used throughout the
framework's own drawing code.
