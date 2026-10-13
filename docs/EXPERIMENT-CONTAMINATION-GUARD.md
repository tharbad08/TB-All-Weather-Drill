# Experimental: preserve contaminated-water blocking

Source branch: `test/contamination-guard`. This is deliberately **not** merged into the September tested `main` baseline until tested in-game.

## Why our original patch bypasses Moddable Weathers

Both mods patch `UndergroundWaterSourceDrill.GetStrengthModifier`. All Weather Drill installs an early Harmony prefix and returns `false` to replace the vanilla strength calculation and allow water flow through drought/badtide weather. That can skip later prefixes, including Moddable Weathers' `StopWaterIfBlockedOrBadwater` which checks `_isBlocking || _underlyingWaterSource.WaterSource.Contamination > 0`. The **weather-blocked** flag is intentionally cleared/ignored, but **water contamination is unrelated to weather blocking and should not be bypassed**.

## Code change

In `AllWeatherDrillPatches.GetStrength`, inject the existing `____underlyingWaterSource` Harmony private field and return strength 0 if its `WaterSource` is null or `Contamination > 0`. This uses the actual `WaterSource.Contamination` member from the pinned reference assemblies rather than reflection or speculative stubs. The guard is ahead of the power check and factor computation. No behavior changes for clean sources or weather classification.

## Tests

CI must compile against the pinned [precompiled reference binaries](https://github.com/kamigari84/Automation_WeatherModifiers_Signals/tree/a4b8b5f82a88866a545e925af5cc1f82aefe496e/bin/Release/netstandard2.1), validate CLR metadata, and generate a clearly labelled **experimental** DLL. CI cannot simulate Harmony ordering or the game's water system.

In a disposable save, test:
1. Clean aquifer in temperate weather, drought, badtide, rain, monsoon, and refreshing; check intended flow and power.
2. Contaminated aquifer in every weather; it must produce **zero**, including after reload in the middle of a weather event.
3. Dry/uninitialized water source, paused and unpowered drills; all should produce zero.
4. Run with and without Moddable Weathers. Check `Player.log` for Harmony field binding errors, `MissingFieldException`, `MissingMethodException`, `TypeLoadException`, and `NullReferenceException`.
5. Test combinations Monsoon + Badtide and Monsoon + Drought. Restore the original tested DLL if anything fails.

## How to revert

Switch to `main` and build/package or use the preserved September 25 tested zip; no main branch source was modified by this experiment.
