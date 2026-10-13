# All Weather Drill — recovered September 2026 baseline

**Stable baseline:** the source behind the previously used **12,800-byte DLL** is restored from [`tharbad08/TimberbornMods` / `build/all-weather-drill-mw-savefix`](https://github.com/tharbad08/TimberbornMods/tree/build/all-weather-drill-mw-savefix/BuildAWD). Original mod by **Ton Wolfe**, [Workshop item 3609443372](https://steamcommunity.com/sharedfiles/filedetails/?id=3609443372). This repository contains a modified version and preserves author credit.

**Start with [docs/MAINTENANCE-HANDOFF.md](docs/MAINTENANCE-HANDOFF.md)** before changes. It documents features, weather logic, dependency versions, source history, build, exact checksums, differences between tested and October experimental DLLs, known bugs, future tests and recovery instructions.

### Build and package
```bash
bash scripts/build.sh --package
```
Requires .NET SDK 8 and git; downloads pinned compile-time Timberborn reference DLLs (not handwritten stubs). The GitHub Actions [build workflow](.github/workflows/build-all-weather-drill.yml) also produces an installable mod artifact. This does not itself constitute an in-game test.

The recovered source is duplicated deliberately in `BuildAWD/AllWeatherDrill.cs` and `AllWeatherDrill/version-1.0.11.2/Patches/all weather drills.cs`; CI enforces exact byte parity. All changes should be made to the canonical source and mirrored before merge.

### Versions and proof
The [historical successful build](https://github.com/tharbad08/TimberbornMods/actions/runs/36129025730) produced a 12,800-byte DLL with SHA-256 `6989408ddfb2ec9662b174471e0afa0e37ee90617c5ac5ca7355bfc8870e2a3a`. The recovered package is byte-for-byte equal to the previously supplied September archive. This source has a reported user-tested history, whereas October stub-based changes were *not* user-tested and are **not** part of the restored canonical source.

**Important compatibility caution:** With Moddable Weathers, our first-priority drill Harmony prefix may bypass its badwater/contamination guard. Treat this as an open issue for a separately tested future patch, not as already fixed.
