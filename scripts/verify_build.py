#!/usr/bin/env python3
"""Basic CLR and feature identity checks of a freshly compiled All Weather Drill DLL.

This checks output format and patch/feature names; it is NOT proof of IL or
runtime equivalence. Compare in-game behavior separately before publishing.
"""
from pathlib import Path
import hashlib
import struct
import sys

EXPECTED_REFERENCE_SHA256 = "6989408ddfb2ec9662b174471e0afa0e37ee90617c5ac5ca7355bfc8870e2a3a"
REQUIRED_SYMBOLS = [
    b"TonWolfe.AllWeatherDrill",
    b"AllWeatherDrillPatches",
    b"CurrentWeatherResolver",
    b"TryReadModdableWeather",
    b"OnEnterFinishedState",
    b"OnHazardousWeatherStarted",
    b"GetStrengthModifier",
    b"PrepareWaterSource",
    b"NormalWeatherScale",
    b"BadtideWeatherScale",
    b"DroughtWeatherScale",
    b"OtherWeatherScale",
    b"PowerScale",
    b"SetInputMultiplierCompat",
    b"UnhideFullModelPermanently",
    b"ModSettingsOwner",
    b"0Harmony",
]

def verify(path: Path):
    data = path.read_bytes()
    if len(data) < 1024 or data[:2] != b"MZ":
        raise ValueError("Not a valid PE DLL")
    pe = struct.unpack_from("<I", data, 0x3c)[0]
    if data[pe:pe + 4] != bytes([80, 69, 0, 0]):
        raise ValueError("PE header absent")
    # IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR indicates a managed CLR image.
    optional = pe + 4 + 20
    magic = struct.unpack_from("<H", data, optional)[0]
    if magic not in (0x10b, 0x20b):
        raise ValueError("Not a supported PE optional header")
    data_directory = optional + (112 if magic == 0x20b else 96)
    clr_rva = struct.unpack_from("<I", data, data_directory + 14 * 8)[0]
    if clr_rva == 0:
        raise ValueError("No CLR runtime header")
    missing = [x.decode() for x in REQUIRED_SYMBOLS if x not in data]
    if missing:
        raise ValueError("Missing expected metadata symbols: " + ", ".join(missing))
    digest = hashlib.sha256(data).hexdigest()
    print(f"PASS: managed DLL, {len(data)} bytes, expected mod and patch symbols present")
    print(f"SHA256: {digest}")
    print("Exact historical artifact:", "YES" if digest == EXPECTED_REFERENCE_SHA256 else "NO (a rebuild may differ)")
    print("CAUTION: signatures and strings do not prove identical IL or in-game behavior.")

if __name__ == "__main__":
    verify(Path(sys.argv[1]))
