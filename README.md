# PCOptimizer

Gaming-focused Windows 11 PC optimizer. A single self-contained exe for Windows 11 24H2 or newer, x64 only, in English and German.
The full plan is `PCOptimizer-plan-v4.md`. This repository has **Milestones 1 to 6** implemented.

What works:

- **Scan:** 41 read-only checks (findings, BIOS and hardware advisor, game access per anti-cheat), each explained in English and German with what was found, why it matters, how to fix it and sources.
- **Tweaks:** 91 catalog tweaks (`src/Optimizer.Core/Catalog/Tweaks/*.json`) with gaming impact (0 to 5), risk, sources and a full explanation page each, plus tweaks built for this PC: per-game NVIDIA profiles, MSI mode and interrupt affinity per device (Expert), startup entries, services, Windows features.
- **Apply recommended:** one confirmation for all fixes of detected problems and the tweaks whose conditions match this PC, each with the reason. Expert, boot-critical, anti-cheat sensitive and not fully reversible items are listed but left out.
- **Change engine:** detect, guard rules, restore point once per session, backup with the first-original rule, apply with per-tweak rollback, verify, change log. Undo per change or all at once, also after a restart for changes built at runtime (their definition is stored with the backup). Values Windows changed since (feature updates) are left alone and reported.
- **Graphics & network:** NVIDIA driver settings through the documented NVAPI driver settings (no undocumented ones), VRR state per display, NIC power saving and interrupt moderation (only keywords the driver declares), DNS presets and an opt-in DNS benchmark.
- **Debloat:** reviewed allowlist of 34 inbox apps, removed for all users and deprovisioned, with Xbox/Game Pass and X3D Game Bar guards, Store links to reinstall, OneDrive uninstall blocked while Known Folder Move or cloud-only files exist, Edge guidance.
- **Cleanup:** 11 categories with sizes, never follows junctions or links, skips files in use, temp files only after 24 hours, the signed-in user's Recycle Bin only, DISM component cleanup, Delivery Optimization cache.
- **Startup (Autoruns-style):** Run keys, Startup folders, logon and boot tasks, services, drivers, Explorer context menu extensions, Winlogon, image file redirects and AppInit, with Authenticode check (embedded and catalog signatures) and an opt-in VirusTotal hash lookup with your own key (stored DPAPI-encrypted).
- **Services & tasks:** start types with explanations for known services; Windows services read-only unless explained; scheduled tasks on and off.
- **Apps & drivers:** 25 apps with verified winget IDs (per-user installers run as the signed-in user), driver list with age and vendor links.
- **Tools:** storage analyzer (largest files and folders, duplicates by SHA-256, Recycle Bin only, Windows/program/game folders protected), Windows optional features, Windows Update component repair, DNS flush, Explorer restart, Winsock reset.
- **Health:** throttle check under load (processor performance limit counter and NVIDIA clock limit reasons), frame time benchmark with PresentMon 2.6.0 (A/B runs, "no measurable difference" when ranges overlap), SFC and DISM with live output, drive health, opt-in sensors (LibreHardwareMonitor, PawnIO on request).
- **Privacy:** a core set of 26 additional privacy tweaks (48 settings) from documented Group Policy and Policy CSP values.
- **UI:** Windows 11 Fluent (WPF-UI): navigation rail, cards, toggle switches, info bars, Segoe UI Variable type ramp, Mica, light and dark mode following Windows, Windows accent color.

## Build and run

```powershell
dotnet build                     # Debug: asInvoker manifest, runs without UAC (TPM and some counters show "Unknown")
dotnet test                      # unit tests + explanation lint (lint = error in Release)
dotnet test -c Release           # what CI should run
dotnet publish src/Optimizer.App -p:PublishProfile=SingleFile   # writes artifacts/publish/PCOptimizer.exe
```

Developer switches (all read-only):

| Switch | Effect |
|---|---|
| `--report <file.md>` | headless scan, writes all findings with explanations and the system info as Markdown |
| `--screenshot <file.png>` | opens the UI, scans, renders the window to PNG and exits |
| `--page <name>` | page for `--screenshot`: Overview, Tweaks, Advisor, Network, Debloat, Cleanup, Startup, Services, Apps, Tools, Health, Changes, Hardware, Settings |
| `--select <id>` | opens a finding or tweak in the details pane (e.g. `A.rebar`) |
| `--lang en\|de`, `--theme System\|Dark\|Light` | override saved preferences |
| `--confirm <tweak id> --confirm-shot <file.png>` | renders the confirmation dialog of a tweak (nothing is applied) |

## Layout

```
src/Optimizer.Core/   scanner, findings, engine, actions, catalog data (Catalog/Data/*.json), explanation pages (Catalog/Docs/{en,de}/*.md),
                      debloat, cleanup, startup, services, apps, tools (storage, features, update repair, PresentMon, sensors)
src/Optimizer.App/    WPF UI (WPF-UI 4.3, CommunityToolkit.Mvvm, Markdig): one page per area, shared change runner
tests/                xUnit: rule tests with mocked hardware, engine tests in a registry sandbox, docs lint, read-only hardware checks (Category=Hardware)
docs/                 third-party notices, explanation style guide, notes from real hardware
```

Data: `%ProgramData%\PCOptimizer` (locked to Administrators and SYSTEM): `backups\` (originals per change), `exports\` (BCD and power plan exports), `logs\`, `tools\` (PresentMon and captures), `settings.json`, `removed-apps.json`, `throttle.json`.

## Testing changes safely

- `dotnet test` runs the engine against a registry sandbox under `HKCU\Software\PCOptimizerTest` and fake system APIs. Nothing on the real system changes.
- `--filter Category=Hardware` runs read-only checks of the real adapters on this PC (power, services, tasks, displays, NVAPI, startup scan, signatures, AppX list, cleanup sizes, drive health, counters, sensors).
- Real apply and undo round trips belong in a Hyper-V VM with checkpoints (plan v4 section 10.2). On a real PC, start with a harmless reversible tweak such as "Show file extensions", then undo it on the Changes page.
