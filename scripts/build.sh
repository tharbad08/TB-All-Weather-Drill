#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

: "${TIMBERBORN_GAME_DIR:?Set TIMBERBORN_GAME_DIR to the Timberborn game installation}"
: "${TIMBERBORN_WORKSHOP_DIR:?Set TIMBERBORN_WORKSHOP_DIR to steamapps/workshop/content/1062090}"

managed="$TIMBERBORN_GAME_DIR/Timberborn_Data/Managed"
test -d "$managed" || { echo "Missing game DLL folder: $managed" >&2; exit 1; }

# Harmony may be placed directly in the Workshop root or a version subfolder.
harmony=$(find "$TIMBERBORN_WORKSHOP_DIR/3284904751" -type f -name '0Harmony.dll' -print -quit 2>/dev/null || true)
modsettings="$TIMBERBORN_WORKSHOP_DIR/3283831040"
test -n "$harmony" && test -f "$harmony" || { echo "Missing Harmony; subscribe to Workshop item 3284904751" >&2; exit 1; }
test -d "$modsettings" || { echo "Missing eMka.ModSettings; subscribe to Workshop item 3283831040" >&2; exit 1; }
command -v dotnet >/dev/null || { echo ".NET SDK (8 or later) required" >&2; exit 1; }

dotnet build BuildAWD/AllWeatherDrill.csproj -c Release \
  -p:GameManagedDir="$managed" \
  -p:HarmonyDll="$harmony" \
  -p:ModSettingsDir="$modsettings"

dll="BuildAWD/bin/Release/netstandard2.1/all weather drills.dll"
test -s "$dll"
python3 scripts/verify_build.py "$dll"
echo "Compiled: $dll"

if [[ "${1:-}" == "--package" ]]; then
  rm -rf dist/AllWeatherDrill
  mkdir -p dist/AllWeatherDrill
  cp -a AllWeatherDrill/version-1.0.11.2 dist/AllWeatherDrill/
  cp "$dll" "dist/AllWeatherDrill/version-1.0.11.2/Patches/all weather drills.dll"
  (cd dist && zip -qr AllWeatherDrill-built.zip AllWeatherDrill)
  echo "Packaged: dist/AllWeatherDrill-built.zip"
fi
