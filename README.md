# All Weather Drill — Timberborn

This repository preserves a **locally modified build** of **All Weather Drill** for Timberborn, including its C# patch source, manifest, changelog and localization.

## Original mod and credit

- **Original mod:** [All Weather Drill — Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3609443372)
- **Original mod author:** **Ton Wolfe**
- **Steam Workshop item ID:** `3609443372`
- **Original behavior:** allows vanilla aquifer drills to keep producing water during droughts and badtides, with adjustable output and power requirements.

**Attribution:** Ton Wolfe created the original mod. The files in this repository are from the locally modified `AllWeatherDrill_ModWeather_SaveFix_v3` archive, **not an untouched upstream release**. The original author's ownership and any upstream licensing conditions are not changed by this repository. No separate source-repository URL or license was provided with the archive.

## Local modifications in this snapshot

This variant adds/contains:

- Per-weather output multipliers for temperate, drought, badtide and otherwise-unrecognized weather.
- Optional **Moddable Weathers** support, including base weather and modifiers: badtide/drought take precedence; rain, refreshing and monsoon normally use temperate scaling.
- Live weather-stage evaluation when calculating drill strength, including after loading a save during a weather event.
- An aquifer-model visibility fix after disabling drought influence.
- A compatibility fallback for changing the drill's mechanical input multiplier.

See the code and mod manifest for the exact behavior. These are claims about the **supplied source**, not independently verified in-game tests.

## Contents

The mod's packaged directory layout is preserved under `AllWeatherDrill/version-1.0.11.2/`:

| Path | Contents |
| --- | --- |
| `manifest.json` | Mod metadata and dependency declarations |
| `Patches/all weather drills.cs` | C# Harmony patch source |
| `Patches/all weather drills.dll` | Binary is present in the supplied ZIP but **not uploaded to this source repository** |
| `Localizations/enUS.csv` | English UI strings |
| `Change Log` | Supplied changelog |

**Required:** Harmony and eMka.ModSettings. **Optional:** ModdableWeathers. The manifest identifies the mod as `TonWolfe.AllWeatherDrill`, version `1.0.5-local`, minimum game version `1.0.11.2`.

## Installing the supplied build

For an installable build use the user's supplied ZIP (which includes the DLL); cloning this source repository alone is insufficient. Use the `AllWeatherDrill` folder as the mod folder so that its `version-1.0.11.2` subfolder, manifest, patches and localization remain together. Avoid running two copies of the same mod ID.

## Source and build status

The supplied archive includes one `.cs` source file plus a compiled `.dll` (the binary has not been committed); **it does not include a `.csproj` or a complete reproducible build setup**. The repository contains the extracted text/source files and this documentation, not a claim that the assembly was rebuilt or tested against the latest Timberborn release.

## Known discrepancy in the supplied package

The supplied `Change Log` says it adds separate Rain, Monsoon and Refreshing flow settings, but the supplied C# source only has one `NormalWeatherScale` for those three weather conditions. The changelog is preserved verbatim for provenance.

## Building from source (new)

A .NET Standard 2.1 project is provided at `BuildAWD/AllWeatherDrill.csproj`. It compiles the exact C# file in the packaged mod directory; it does not maintain a second source copy.

The build references the game's installed `Timberborn_Data/Managed` assemblies plus **Harmony** and **eMka.ModSettings** from Steam Workshop. These dependencies are *not redistributed* here.

```bash
export TIMBERBORN_GAME_DIR="$HOME/.local/share/Steam/steamapps/common/Timberborn"
export TIMBERBORN_WORKSHOP_DIR="$HOME/.local/share/Steam/steamapps/workshop/content/1062090"
bash scripts/build.sh --package
```

Alternatively use `dotnet build BuildAWD/AllWeatherDrill.csproj -c Release -p:GameManagedDir=... -p:HarmonyDll=... -p:ModSettingsDir=...` with your installed DLL directories. The script verifies that the output is a managed PE assembly with the expected patch and weather-related metadata, and can package the newly built DLL.

**Important verification status:** The exact supplied historical DLL has SHA-256 `6989408ddfb2ec9662b174471e0afa0e37ee90617c5ac5ca7355bfc8870e2a3a` and size 12,800 bytes. This snapshot's original and installable ZIP copies were byte-for-byte identical. GitHub Actions successfully compiled the source on 2026-10-13 (run 38327665255) **against compile-time API stubs**, not the real game assemblies. The resulting DLL has not been validated in-game against Timberborn 1.1. A newly built DLL may have a different hash despite equivalent behavior. The verification script checks structure and expected metadata only, **not IL-level or in-game equivalence**.

## GitHub Actions build

[Build workflow](.github/workflows/build-all-weather-drill.yml) runs on push/PR/manual dispatch and publishes `AllWeatherDrill-compiled-reference-build` as a downloadable artifact. [First successful run](https://github.com/tharbad08/TB-All-Weather-Drill/actions/runs/38327665255).

**Caution:** This build uses locally defined type-signature placeholders, following the strategy used by TB Optimizer. It confirms compilation and key CLR metadata; it **cannot prove the output is equivalent to the original DLL** nor that the DLL will load successfully into real Timberborn 1.1. For a final production build, compile using the real Timberborn 1.1 and mod assemblies and compare behavior. The source-generated DLL's hash need not match the reference even if equivalent.

## Moddable Weathers 11.0.1 source integration audit (2026-10-13)

Upstream source: [datvm/TimberbornMods / ModdableWeathers](https://github.com/datvm/TimberbornMods/tree/master/ModdableWeathers), inspected at commit `8bc6fa14dfa8ccd299df98678971e629a2d1adc2`. Manifest: `ModdableWeathers` version **11.0.1**, minimum Timberborn **1.1.1**.

- Replaced `ModdableWeatherService` exposes `WeatherDayService` (a `WeatherCycleService`), whose `CurrentWeather` and `CurrentWeatherModifiers` reflect the live stage, including resumed saves.
- Moddable Weathers also Harmony-patches `UndergroundWaterSourceDrill.GetStrengthModifier` to return zero if blocked or contaminated. **Important:** All Weather Drill's high-priority early-return prefix can bypass that prefix. The updated source repeats the upstream `WaterSource.Contamination > 0` safeguard in its own prefix, by runtime reflection, so the protection is not dependent on another prefix executing. Verify its behavior with actual contaminated aquifers; a failed reflection lookup still requires in-game scrutiny.
- IDs in source: `TemperateWeather`, `DroughtWeather`, `BadtideWeather`, `Rain`, `Monsoon`, `SurprisinglyRefreshing`; modifiers `Drought`, `Badtide`, `Rain`, `Monsoon`, `Refreshing`.
- `CurrentWeatherResolver` now walks inherited private fields/properties, handles missing/null observers, does not treat an unreadable modifier list as empty, and uses the configured **Other** multiplier if the modded weather service cannot be read. These changes are in source and require a **new compiled DLL** to take effect.
- No hard dependency on Moddable Weathers was added. Without it, vanilla hazardous-weather observer checks remain in use.
- CI workflow [audit-moddable-weathers.yml](.github/workflows/audit-moddable-weathers.yml) checks the pinned upstream source contract. It is **not** a substitute for runtime or API-signature verification with actual game DLLs.
- Keep backups and test save/load during Rain, Monsoon, Drought, Badtide, mixed base/modifier weather and after disabling Moddable Weathers. **Do not ship the new stub-built DLL as tested.**

This upstream audit documents the precise interfaces and outstanding behavior risks so future source changes can be checked before release.
