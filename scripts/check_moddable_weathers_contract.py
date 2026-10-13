#!/usr/bin/env python3
"""Check All Weather Drill's optional weather API contract against upstream source.

Checks are deliberately exact: drift should fail CI, not silently switch weather
category. This is a source contract check, NOT a Unity runtime test.
"""
from pathlib import Path
import re
import sys

root=Path(__file__).resolve().parents[1]
source=(root/"AllWeatherDrill/version-1.0.11.2/Patches/all weather drills.cs").read_text()
upstream=Path(sys.argv[1]) if len(sys.argv)>1 else root/"upstream/ModdableWeathers"
checks={
"live cycle property": (upstream/"Services/ModdableWeatherService.cs","readonly WeatherCycleService WeatherDayService"),
"live weather property": (upstream/"Cycles/WeatherCycleService.cs","CurrentWeather => CurrentStage.Stage.Weather"),
"live modifiers property": (upstream/"Cycles/WeatherCycleService.cs","CurrentWeatherModifiers => CurrentStage.Stage.WeatherModifiers"),
"weather id": (upstream/"Weathers/IModdableWeather.cs","string Id { get; }"),
"modifier id": (upstream/"WeatherModifiers/IModdableWeatherModifier.cs","string Id { get; }"),
"drill prefix collision": (upstream/"Patches/Game/UndergroundWaterSourceDrillPatches.cs","StopWaterIfBlockedOrBadwater"),
"modded service replacement":(upstream/"Services/ModdableWeatherService.cs","[ReplaceSingleton]"),
}
for name,(path,needle) in checks.items():
    if needle not in path.read_text(encoding="utf-8-sig"): raise SystemExit("FAIL upstream API changed: "+name)
for key in ("TemperateWeather","Rain","Monsoon","SurprisinglyRefreshing","BadtideWeather","DroughtWeather","Badtide","Drought","Refreshing"):
    if f'"{key}"' not in source: raise SystemExit("FAIL missing known weather/modifier ID "+key)
for part in ("IsModdableWeatherService","BindingFlags.DeclaredOnly","if (currentMods == null) return false;",
             "if (moddableService) return DrillWeatherKind.Other;",
             "if (observer == null) return DrillWeatherKind.Other;"):
    if part not in source: raise SystemExit("FAIL missing safe weather resolution rule: "+part)
print("PASS: Moddable Weathers 11.0.1 source contract matches known drill integration")
print("NOTE: live Unity game validation still required")
