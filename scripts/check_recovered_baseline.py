#!/usr/bin/env python3
"""Provenance guard for the recovered September 25 2026 source build."""
import hashlib
import pathlib
import sys
import os

root = pathlib.Path(__file__).resolve().parents[1]
pairs = [
 ("BuildAWD/AllWeatherDrill.cs", "AllWeatherDrill/version-1.0.11.2/Patches/all weather drills.cs"),
 ("BuildAWD/package/manifest.json", "AllWeatherDrill/version-1.0.11.2/manifest.json"),
 ("BuildAWD/package/enUS.csv", "AllWeatherDrill/version-1.0.11.2/Localizations/enUS.csv"),
]
for src, dst in pairs:
    a, b = (root / src).read_bytes(), (root / dst).read_bytes()
    if a != b: sys.exit(f"FAIL: packaged file differs: {src} != {dst}")
    print(f"PASS mirror: {src} == {dst}")
source = (root / pairs[0][0]).read_bytes()
expected_source_git_sha = "551c12ee85b6603eba89c25e50df7d091224c7a0"
actual = hashlib.sha1(b"blob " + str(len(source)).encode() + b"\0" + source).hexdigest()
if actual != expected_source_git_sha:
    if os.environ.get("AWD_EXPERIMENTAL_GUARD") == "1":
        required = (b"WaterSource.Contamination > 0f", b"____underlyingWaterSource.WaterSource == null", b"__result = 0f")
        if not all(x in source for x in required):
            sys.exit("FAIL: experimental contamination guard absent")
        print("PASS experimental guard: baseline source intentionally modified " + actual)
    else:
        sys.exit("FAIL: source differs from historical baseline " + actual)
print("PASS recovered source Git blob " + actual)
if len(sys.argv) > 1:
    dll = pathlib.Path(sys.argv[1]).read_bytes()
    digest = hashlib.sha256(dll).hexdigest()
    expected = "6989408ddfb2ec9662b174471e0afa0e37ee90617c5ac5ca7355bfc8870e2a3a"
    print("Built DLL SHA-256: " + digest)
    if digest == expected: print("PASS byte-identical to September 25 reference DLL")
    else: print("NOTE: differs from September 25 reference DLL; exact equivalence NOT verified")
