#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Recreates the September 2026 GitHub Actions build as closely as practical.
# Uses precompiled reference assemblies from a pinned third-party repository.
# Never substitutes placeholder Timberborn/Bindito stub assemblies.
ref_sha="a4b8b5f82a88866a545e925af5cc1f82aefe496e"
refs="${PUBLIC_REF_DIR:-}"
if [[ -z "$refs" ]]; then
  tmp=$(mktemp -d)
  trap 'rm -rf "$tmp"' EXIT
  git clone --filter=blob:none https://github.com/kamigari84/Automation_WeatherModifiers_Signals.git "$tmp/tbrefs"
  git -C "$tmp/tbrefs" checkout "$ref_sha"
  refs="$tmp/tbrefs/bin/Release/netstandard2.1"
fi
[[ -d "$refs" ]] || { echo "Reference directory missing: $refs" >&2; exit 1; }
command -v dotnet >/dev/null || { echo "Install .NET SDK 8" >&2; exit 1; }
python3 scripts/check_recovered_baseline.py
dotnet build BuildAWD/AllWeatherDrill.csproj -c Release "-p:PublicRefDir=$refs" --nologo
dll="BuildAWD/bin/Release/netstandard2.1/all weather drills.dll"
python3 scripts/verify_build.py "$dll"
python3 scripts/check_recovered_baseline.py "$dll"
if [[ "${1:-}" == "--package" ]]; then
  root="dist/AllWeatherDrill/version-1.0.11.2"
  rm -rf dist/AllWeatherDrill
  mkdir -p "$root/Patches" "$root/Localizations"
  cp "$dll" "$root/Patches/all weather drills.dll"
  cp BuildAWD/AllWeatherDrill.cs "$root/Patches/all weather drills.cs"
  cp BuildAWD/package/manifest.json "$root/manifest.json"
  cp BuildAWD/package/enUS.csv "$root/Localizations/enUS.csv"
  cp "AllWeatherDrill/version-1.0.11.2/Change Log" "$root/Change Log"
  (cd dist && zip -qr AllWeatherDrill-recovered-build.zip AllWeatherDrill)
  echo "dist/AllWeatherDrill-recovered-build.zip"
fi
