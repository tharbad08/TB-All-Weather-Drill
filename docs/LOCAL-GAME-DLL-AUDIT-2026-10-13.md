# Local game DLL audit — 2026-10-13

This report documents an inspection of the user's **September 25, 2026** Timberborn build-reference archive `timberborn-build-refs-20260925-130502(1).zip`. **The archive and all proprietary game DLLs remain outside GitHub.** Only inspected metadata, hashes, and conclusions are preserved here.

## Archive integrity and version boundaries

- ZIP CRC check: **passed**.
- `SHA256SUMS.txt`: **555 entries verified; zero mismatches, zero missing files**.
- 502 game-side files, 52 Workshop-side files, plus checksum and manifest. The game DLLs are dated September 3, 2026 inside the ZIP. This does **not** prove the user has not updated since.
- Bundled Workshop Moddable Weathers (version-1.1) manifest: **11.0.0**; current upstream source reviewed separately: **11.0.1**. These bundled mod binaries may be out-of-date, and **were not treated as the authoritative mod contract**.
- Bundled ModdableTimberborn (version-1.1) manifest: **11.1.2**. Treat bundled Workshop binaries as informational, not current.
- User confirms the base-game DLLs are suitable references. The workshop libraries may not be.

## Exact CLR members verified from *base-game DLL metadata*

| Assembly | Declaring type | Verified member | CLR signature / result |
| --- | --- | --- | --- |
| `Timberborn.GameWaterSourceSystem` | `UndergroundWaterSourceDrill` | `_underlyingWaterSource` | instance field, type `UnderlyingWaterSource` |
| same | same | `_mechanicalNode` | instance field |
| same | same | `_hazardousWeatherObserver` | instance field |
| same | same | `_blockableObject` | instance field |
| same | same | `_isBlocking` | instance **bool** field |
| same | same | `GetStrengthModifier()` | returns **float**, signature `20000c` |
| same | same | `OnEnterFinishedState()` | instance method |
| same | same | `OnHazardousWeatherStarted(...)` | instance method |
| `Timberborn.WaterSourceSystem` | `UnderlyingWaterSource` | `get_WaterSource()` | returns `WaterSource` |
| same | `WaterSource` | `get_Contamination()` | returns **float**, signature `20000c` |
| same | `HazardousWeatherObserver` | `_weatherService` | instance field |
| `Timberborn.MechanicalSystem` | `MechanicalNode` | `get_ActiveAndPowered()` | returns **bool**, signature `200002` |
| same | same | `get_PowerEfficiency()` | returns **float**, signature `20000c` |
| same | same | `SetInputMultiplier(float)` | present, signature `2001010c` |

The experimental build `AllWeatherDrill_ContaminationGuard_Install.zip` has 12,800-byte `all weather drills.dll`, SHA-256 `afca936b6f87f97a9a9691bcd1c100977f67d6a16ab6033b9fb49df0f0ab134d`. Its `GetStrength` has **four parameters** (including `____underlyingWaterSource`) and directly references `get_Contamination`. The previously used September DLL lacked this call. The new field injection and contamination check match the **uploaded game's** CLR declarations.

## Difference from the public CI reference repository

The pinned [public reference bundle](https://github.com/kamigari84/Automation_WeatherModifiers_Signals/tree/a4b8b5f82a88866a545e925af5cc1f82aefe496e/bin/Release/netstandard2.1) is **not byte-identical** to the user's base game. Examples:

| Assembly | Uploaded game bytes | Public CI reference bytes |
| --- | ---: | ---: |
| `Timberborn.GameWaterSourceSystem.dll` | 12,800 | 13,312 |
| `Timberborn.WaterSourceSystem.dll` | 41,472 | 40,960 |
| `Timberborn.MechanicalSystem.dll` | 44,544 | 43,008 |
| `Timberborn.BlockObjectModelSystem.dll` | 15,360 | 15,872 |
| `Bindito.Core.dll` | 55,296 | 55,296 (same Git blob) |

The published CI build **did not compile against the user's uploaded game DLLs**. This audit checks that critical declarations it uses *also exist* in those actual binaries. Binary size mismatches warrant further verification before calling a build fully production-tested.

## Current conclusion

**Source-level and CLR metadata compatibility of the contamination guard with the user's archived base-game DLLs is confirmed.** This is stronger evidence than a stub-only build, but is **not** an in-game execution test. The original September source remains preserved on `main`; the contamination fix stays in experimental PR #1 until a colony test passes.

Test powered clean, polluted, null-source and unpowered drills through temperate, drought, badtide, modifiers and save/reload; compare to known September behavior. Review `Player.log` for Harmony patch errors, including failure to inject `____underlyingWaterSource`.

## Future work

- Prefer building against the user's **currently installed real game assemblies** where available, rather than relying exclusively on the public pinned bundle. Do not commit the DLLs to GitHub.
- For mod compatibility continue to use current upstream [datvm ModdableWeathers source](https://github.com/datvm/TimberbornMods/tree/master/ModdableWeathers), not the older bundled 11.0.0 binary.
- Record game and Workshop mod versions, current API signatures, DLL SHA-256 and real in-game test result whenever updating.
