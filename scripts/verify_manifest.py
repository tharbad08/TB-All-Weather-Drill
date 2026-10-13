#!/usr/bin/env python3
"""Check manifest, game version folder, source identity and assembly version.

Metadata only: this does not replace runtime testing of Harmony patches.
"""
from pathlib import Path
import json
import re
import sys
from xml.etree import ElementTree

root=Path(__file__).resolve().parents[1]
manifest_path=root/"BuildAWD/package/manifest.json"
mod_path=root/"AllWeatherDrill/version-1.1/manifest.json"
source_path=root/"BuildAWD/AllWeatherDrill.cs"
project_path=root/"BuildAWD/AllWeatherDrill.csproj"
source=source_path.read_text()
settings=json.loads(manifest_path.read_text())
if manifest_path.read_bytes()!=mod_path.read_bytes():
    sys.exit("FAIL: packaged manifest differs from canonical manifest")
assert settings["Version"]=="1.1.38", "wrong mod version"
assert settings["MinimumGameVersion"]=="1.1", "wrong minimum game version"
assert settings["Id"]=="Tharbad08.AllWeatherDrill", "wrong mod ID"
assert settings["Name"]=="All Weather Drill (ModdableWeathers)", "wrong mod display name"
assert {x["Id"] for x in settings["RequiredMods"]}=={"Harmony","eMka.ModSettings"}, "unexpected required mods"
assert {x["Id"] for x in settings["OptionalMods"]}=={"ModdableWeathers"}, "Moddable Weathers should remain optional"
assert "Contamination > 0f" in source, "missing contamination gate"
assert 'new Harmony("Tharbad08.AllWeatherDrill")' in source, "Harmony identity not migrated"
assert 'protected override string ModId { get; } = "Tharbad08.AllWeatherDrill";' in source, "settings identity not migrated"
xml=ElementTree.parse(project_path).getroot()
def value(name): return xml.findtext("./PropertyGroup/"+name)
assert value("Version")==settings["Version"], "assembly package version and manifest differ"
assert value("AssemblyVersion")=="1.1.38.0", "wrong assembly version"
print("PASS: mod manifest, ID, version, optional dependencies, source and assembly identity consistent")
print("NOTE: changing the mod ID from TonWolfe.AllWeatherDrill resets the previous settings namespace")
