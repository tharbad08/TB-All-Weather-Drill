# All Weather Drill — Timberborn

This repository preserves a **locally modified build** of **All Weather Drill** for Timberborn, including its C# patch source, compiled assembly, manifest, changelog and localization.

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
| `Patches/all weather drills.dll` | Compiled mod assembly from the supplied archive |
| `Localizations/enUS.csv` | English UI strings |
| `Change Log` | Supplied changelog |

**Required:** Harmony and eMka.ModSettings. **Optional:** ModdableWeathers. The manifest identifies the mod as `TonWolfe.AllWeatherDrill`, version `1.0.5-local`, minimum game version `1.0.11.2`.

## Installing the supplied build

Use the `AllWeatherDrill` folder as the mod folder so that its `version-1.0.11.2` subfolder, manifest, patches and localization remain together. Avoid running two copies of the same mod ID.

## Source and build status

The archive includes one `.cs` source file plus the compiled `.dll`; **it does not include a `.csproj` or a complete reproducible build setup**. The repository is a faithful archival copy plus this documentation, not a claim that the assembly was rebuilt or tested against the latest Timberborn release.
