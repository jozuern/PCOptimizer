<p align="center"><img src="docs/brand/pcoptimizer-icon-256.png" width="96" alt="PCOptimizer icon"></p>

# PCOptimizer

A Windows 11 PC optimizer, gaming first, with profiles for laptops, battery, office, quiet and older PCs. It explains every finding and every change, backs up what it changes and can undo it. A single exe for Windows 11 24H2 or newer, x64 only, in English and German.

> **Status: preview (0.3).** Apply and undo are tested against a registry sandbox, not yet on many real PCs. Create a restore point or a backup before you change anything, and start with the recommended items. Laptop profiles, battery checks and AMD-specific checks are not yet checked on real hardware.

## Download

Get `PCOptimizer.exe` from the [Releases](https://github.com/jozuern/PCOptimizer/releases) page and compare its SHA-256 with the `.sha256` file next to it (`Get-FileHash PCOptimizer.exe`). The exe asks for administrator rights because it changes system settings. It is not code signed yet, so Windows SmartScreen may warn on first start.

## What it does

- **Scan:** 46 read-only checks (problems, BIOS and hardware advice, game access per anti-cheat). Each comes with what was found, why it matters, how to fix it and sources, in English and German.
- **Tweaks:** 99 catalog tweaks with gaming impact from 0 to 5, risk and sources, plus tweaks built for this PC: per-game NVIDIA profiles, MSI mode and interrupt affinity per device (Expert), startup entries, services, Windows features.
- **Profiles:** Gaming, Laptop gaming, Battery, Office, Quiet and cool, Older PC. A profile changes nothing by itself; it decides what "Apply recommended" includes and which impact is shown.
- **Apply recommended:** one confirmation for the fixes of detected problems and the tweaks that fit this PC, each with its reason. Expert, boot-critical, anti-cheat sensitive and not fully reversible items are never included.
- **Undo:** every change is backed up first (plus a restore point once per session), verified after applying and listed on the Changes page with Undo, also after a restart. After a Windows update the app lists changes Windows reset and offers to apply them again.
- **Graphics & network:** NVIDIA driver settings through the documented driver settings API, VRR state, network adapter power saving, DNS presets and an opt-in DNS benchmark.
- **Debloat, cleanup, startup, services:** a reviewed list of 34 inbox apps, 11 cleanup categories that never follow links, an Autoruns-style startup list with signature checks, service start types with explanations.
- **Tools and health:** storage analyzer, Windows features, Windows Update repair, SFC and DISM, a frame time benchmark with PresentMon, throttle check, drive health, opt-in sensors.

The app does not use undocumented driver settings, never recommends turning off security features that anti-cheats require, and keeps changes that weaken security or touch the boot configuration in Expert mode.

## Privacy

No telemetry, no accounts. The app goes online only for features you start or turn on (update check, VirusTotal lookup, DNS benchmark, app installs). Details: [docs/PRIVACY.md](docs/PRIVACY.md).

## License

PCOptimizer is free for personal and other noncommercial use under the [PolyForm Strict License 1.0.0](LICENSE). Changing, redistributing or selling it requires the author's permission. It comes without warranty. Commercial use: ask via an issue.

The exe contains third-party components under their own licenses (MIT, BSD-2-Clause, MPL-2.0, Apache-2.0); the full texts are in the app under Settings > About > Licenses and listed in [docs/THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md).

## Credits

PCOptimizer was built with the help of Claude, an AI assistant by Anthropic.

## Feedback and security

Bugs and ideas: open an issue. Security problems: see [SECURITY.md](SECURITY.md). Building from source and project layout: [CONTRIBUTING.md](CONTRIBUTING.md).
