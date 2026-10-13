# All Weather Drill — maintenance handoff (2026-10-13)

## Canonical state and provenance
The **canonical source** is recovered from [TimberbornMods / build/all-weather-drill-mw-savefix / BuildAWD](https://github.com/tharbad08/TimberbornMods/tree/build/all-weather-drill-mw-savefix/BuildAWD), branch head `19e4b64d910fd50455460a8197b33bec25a78228`.
The exact tested source blob is `551c12ee85b6603eba89c25e50df7d091224c7a0` (11,943 bytes). Mirrored at `BuildAWD/AllWeatherDrill.cs` and `AllWeatherDrill/version-1.0.11.2/Patches/all weather drills.cs`.
Original mod credit: Ton Wolfe, [Steam Workshop 3609443372](https://steamcommunity.com/sharedfiles/filedetails/?id=3609443372). This is a modified build; no upstream rights are transferred.

**Historical validation**: GitHub Actions [run 36129025730](https://github.com/tharbad08/TimberbornMods/actions/runs/36129025730) succeeded 2026-09-25 with the recovered BuildAWD code. Its archived package was recovered and checked against the earlier locally used package byte-for-byte. DLL length **12,800**, SHA-256 **6989408ddfb2ec9662b174471e0afa0e37ee90617c5ac5ca7355bfc8870e2a3a**. Historical in-game testing was reported by the user; CI compilation itself is not a game test. Preserve this as a baseline rather than rewriting it.

## Build environment
- .NET SDK **8**, target `netstandard2.1`, `LangVersion=10.0`; output assembly `all weather drills.dll`.
- Harmony **2.3.3** via `Lib.Harmony` NuGet.
- Reference DLLs from [kamigari84/Automation_WeatherModifiers_Signals](https://github.com/kamigari84/Automation_WeatherModifiers_Signals), pinned to commit `a4b8b5f82a88866a545e925af5cc1f82aefe496e`. These are precompiled reference files, **not** invented Timberborn stubs. They are compile inputs only, not shipped dependencies. Game API correctness still depends on installed game version.
- `bash scripts/build.sh --package` builds and packages a **single** `AllWeatherDrill/` mod folder. `PUBLIC_REF_DIR` can override downloaded references. Workflow `.github/workflows/build-all-weather-drill.yml` repeats these inputs.
- Package layout: `AllWeatherDrill/version-1.0.11.2/{manifest.json,Change Log,Localizations/enUS.csv,Patches/all weather drills.cs,Patches/all weather drills.dll}`.
- Mod ID `TonWolfe.AllWeatherDrill`; requirements Harmony and eMka.ModSettings; Moddable Weathers **optional**. Avoid two simultaneous installations with the same mod ID.

## What the baseline code does
- Harmony patch to `UndergroundWaterSourceDrill.OnEnterFinishedState`: unblocks drill, disables drought influence, restores the aquifer model; sets mechanical input multiplier with reflected setter/field fallback.
- Prefix `OnHazardousWeatherStarted`: prepares water source and prevents vanilla weather blocking.
- Prefix `GetStrengthModifier`: requires `MechanicalNode.ActiveAndPowered`, multiplies power efficiency by weather scale and overrides vanilla result.
- Reads `HazardousWeatherObserver._weatherService`, then optional replaced `WeatherDayService.CurrentWeather` and `CurrentWeatherModifiers` by reflection, on every calculation rather than depending only on a weather-transition event; this is important when resuming a save mid-event.
- Badtide as base weather/modifier takes priority over Drought, then Temperate/Rain/Monsoon/Refreshing, then Other. Default multipliers: Temperate **0.7**, Badtide **0.6**, Drought **0.4**, Other **0.6**, power **1.5**; capped 0..20.
- Monsoon, Rain and Refreshing use the **Temperate multiplier** in the baseline; the historical Change Log incorrectly says separate settings exist. Preserve this distinction in any UI work.

## Moddable Weathers compatibility
Reference [datvm/TimberbornMods/ModdableWeathers](https://github.com/datvm/TimberbornMods/tree/master/ModdableWeathers), source audited at `8bc6fa14dfa8ccd299df98678971e629a2d1adc2`, manifest version 11.0.1. Actual live APIs: `ModdableWeatherService.WeatherDayService`, `WeatherCycleService.CurrentWeather`, `CurrentWeatherModifiers`, each weather/modifier's `Id`.
Moddable Weathers patches the same drill: `StopWaterIfBlockedOrBadwater` prevents contaminated-water production. Our prefix may short-circuit that protection. This is a **risk, not a tested resolution**. Do not silently merge October experiments into this baseline.
A separate upstream contract audit `.github/workflows/audit-moddable-weathers.yml` checks pinned upstream APIs but does not execute Timberborn.

## What NOT to repeat
In October, this separate repo tried handwritten `ReferenceStubs/` projects. The first DLL compiled to 20,480 bytes but included wrong CLR type ownership and method signatures (e.g., observer in the wrong assembly, `GetComponent` in Unity instead of Timberborn BaseComponent, Bindito binding method). A corrected stub variant compiled but remained untested. An October 21,504-byte variant changed reflection behavior, added a contamination safeguard and passed source-contract checks, **but it was not proven in-game**. As of this restoration, these variants are superseded for baseline builds and stubs were removed. Review old Git commits or the historical workflow runs if revisiting those experiments; do not represent their behavior as user-tested.
Other Timberborn lessons: a prior `_pausableBuilding` injection failed on v1.1; prefer the present `MechanicalNode.ActiveAndPowered` path. Invalid IL and `TypeLoadException` from other mods are reminders that compiling against fake APIs is not runtime validation.

## Before next release
1. Compare `scripts/check_recovered_baseline.py` output and the DLL's hash against the September reference.
2. Inspect any upstream Timberborn 1.1, Harmony, eMka.ModSettings and Moddable Weathers changes for CLR member ownership, signatures and Harmony patch order.
3. Re-test in a backup colony: vanilla temperate, drought, badtide; rain, refreshing, monsoon; each as base weather and modifier; combinations (especially Monsoon + Badtide/Drought); **load save during weather**, transitions; unpowered/paused drills, water contamination, aquifer visibility; with Moddable Weathers disabled.
4. If fixing contamination/prefix ordering, use a **separate experimental branch**, run compile + reflection/API checks and gameplay tests. Never modify canonical baseline as a speculative fix.
5. Record actual game version, mod versions, exact test/save conditions, result and `Player.log` with each validation run. No claim of game-tested equivalence without an actual game test.

## Repository map
| File | Purpose |
| --- | --- |
| `BuildAWD/AllWeatherDrill.cs` | Canonical recovered tested source |
| `BuildAWD/AllWeatherDrill.csproj` | Original .NET/Harmony build configuration |
| `BuildAWD/package/` | Canonical manifest/localization inputs |
| `AllWeatherDrill/version-1.0.11.2/` | Mirrored installable layout (DLL produced at build time) |
| `scripts/build.sh` | Local build and packaging |
| `scripts/check_recovered_baseline.py` | Byte parity and source provenance checks |
| `scripts/verify_build.py` | Structural DLL sanity checks; **not** in-game validation |
| `.github/workflows/build-all-weather-drill.yml` | Automated build against pinned real reference binaries |
| `.github/workflows/audit-moddable-weathers.yml` | Optional upstream source API contract audit |


## Recovery validation (2026-10-13)
- The restored [GitHub Actions run 38331596864](https://github.com/tharbad08/TB-All-Weather-Drill/actions/runs/38331596864) completed successfully, using the pinned external reference set and Harmony 2.3.3. Source, manifest, and localization mirrors passed strict byte checks.
- New DLL: **12,800 bytes**, SHA-256 `73be5678e3fb725021fd887e75b3c99a5eff15e08093851c5c1b47f89826263b`. Its hash does **not** match the September reference DLL `6989408ddfb2ec9662b174471e0afa0e37ee90617c5ac5ca7355bfc8870e2a3a`. Thus the new build is successful, **not byte-identical** and **not separately game-tested**. Compiler/build-path metadata is a possible explanation, not a proven root cause; compare IL/metadata before claiming full equivalence.
- The September compiled DLL from the historical Actions artifact has been preserved separately as the known previously used build, for installation or future binary diff. Do not overwrite or relabel it with the newer generated DLL.
- [Moddable Weathers source-contract audit](https://github.com/tharbad08/TB-All-Weather-Drill/actions/runs/38331596815) passed after changing the audit to check the actual recovered baseline rather than the abandoned October experimental guards.
