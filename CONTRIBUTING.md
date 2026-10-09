# Development

Notes for working on PCOptimizer. For what the app does, see [README.md](README.md).

## Contributions

PCOptimizer is source-available under the [PolyForm Strict License 1.0.0](LICENSE), not open source: the license does not allow changed versions or redistribution. Bug reports and ideas are welcome as issues. Code contributions only by prior agreement with the author; please open an issue first. Security problems: see [SECURITY.md](SECURITY.md).

## Build and run

```powershell
dotnet build                     # Debug: asInvoker manifest, runs without UAC (TPM and some counters show "Unknown")
dotnet test                      # unit tests + explanation lint (lint = error in Release)
dotnet test -c Release --filter "Category!=Hardware"   # what CI runs
dotnet publish src/Optimizer.App -p:PublishProfile=SingleFile   # writes artifacts/publish/PCOptimizer.exe
```

Developer switches (all read-only):

| Switch | Effect |
|---|---|
| `--report <file.md>` | headless scan, writes all findings with explanations and the system info as Markdown |
| `--screenshot <file.png>` | opens the UI, scans, renders the window to PNG and exits |
| `--page <name>` | page for `--screenshot`: Overview, Tweaks, Advisor, Network, Debloat, Cleanup, Startup, Services, Apps, Tools, Health, Changes, Hardware, Settings |
| `--shot-scanning <file.png>` | renders the window while the first scan still runs (score placeholders) |
| `--pane closed` | collapses the navigation rail before the screenshot |
| `--scroll end` | scrolls the page to the end before the screenshot (for example Settings > About) |
| `--select <id>` | opens a finding or tweak in the details pane (e.g. `A.rebar`) |
| `--lang en\|de`, `--theme System\|Dark\|Light` | override saved preferences |
| `--confirm <tweak id> --confirm-shot <file.png>` | renders the confirmation dialog of a tweak (nothing is applied) |
| `--licenses-shot <file.png>` | renders the Licenses window |
| `--switch-theme Light\|Dark` | switches the theme while the page is open, before the screenshot (finds text that keeps the old colors) |
| `--perf <file.txt>` | times page switches, filters, profile changes and a full rebuild until the UI is idle, writes the result and exits |
| `--profile <id>` | start with a profile: gaming, laptopGaming, battery, office, quiet, lowEnd (saved only when changed in the app) |
| `--preview-drift on` | shows the "changes reset" banner with sample entries (nothing is read or changed) |
| `--expert on` | Expert mode for this session |

## Layout

```
src/Optimizer.Core/   scanner, findings, engine, actions, catalog data (Catalog/Data/*.json), explanation pages (Catalog/Docs/{en,de}/*.md),
                      debloat, cleanup, startup, services, apps, tools (storage, features, update repair, PresentMon, sensors), release check
src/Optimizer.App/    WPF UI (WPF-UI 4.3, CommunityToolkit.Mvvm, Markdig): one page per area, shared change runner;
                      Licenses/ holds every license text that ships in the exe (licenses.json lists them)
tests/                xUnit: rule tests with mocked hardware, engine tests in a registry sandbox, docs lint, read-only hardware checks (Category=Hardware)
docs/                 privacy, third-party notices, VM test plan, explanation style guide, notes from real hardware, brand (icon, logo, colors)
.github/workflows/    CI (build, tests without Category=Hardware, single-exe artifact) and the tag-triggered release
```

Data: `%ProgramData%\PCOptimizer` when elevated (locked to Administrators and SYSTEM, links removed on start); Debug runs without admin rights use `%LocalAppData%\PCOptimizer` instead: `backups\` (originals per change), `exports\` (BCD and power plan exports), `logs\`, `tools\` (PresentMon and captures), `settings.json` (language, theme, Expert mode, profile, update check), `removed-apps.json`, `throttle.json`.

Catalog data in `src/Optimizer.Core/Catalog/`: `Tweaks/*.json` (tweaks), `Data/profiles.json` (profiles: per-profile impact of tweaks and findings, recommendations, what works against each), `Data/labels.json` (generated text, EN and DE), `Docs/{en,de}/*.md` (one explanation page per tweak and check). Entries marked `verified: false` (BIOS menu paths, one anti-cheat) say so on their explanation page.

## Testing changes safely

- `dotnet test` runs the engine against a registry sandbox under `HKCU\Software\PCOptimizerTest` and fake system APIs. Nothing on the real system changes.
- `--filter Category=Hardware` runs read-only checks of the real adapters on this PC (power, services, tasks, displays, NVAPI, startup scan, signatures, AppX list, cleanup sizes, drive health, counters, sensors).
- Real apply and undo round trips belong in a Hyper-V VM with checkpoints: follow [docs/vm-test-plan.md](docs/vm-test-plan.md). On a real PC, start with a harmless reversible tweak such as "Show file extensions", then undo it on the Changes page.
- Not yet checked on real hardware: the laptop profiles and battery checks (tested with simulated laptops only), battery capacity readings, Wi-Fi band, AMD-specific checks.

## New dependencies

Every NuGet package that ships in the exe needs its license text in `src/Optimizer.App/Licenses/` and an entry in `licenses.json`. The test `EveryShippedPackageHasItsLicense` fails until it is there. Add it to `docs/THIRD-PARTY-NOTICES.md` as well.

## Releases

Every commit raises the patch version (0.3.0 to 0.3.1) through the pre-commit hook in `.githooks`. Enable it once per clone with `git config core.hooksPath .githooks`. Change `<Version>` by hand for a new minor or major version; the hook keeps a version changed in the same commit. Skip it for one commit (for example `--amend`) with `SKIP_VERSION_BUMP=1 git commit`.

1. Pick the commit to release; its `<Version>` in `src/Optimizer.App/Optimizer.App.csproj` is the release version. The tag must match it, or the workflow stops.
2. Tag and push: `git tag v0.4.0` and `git push origin v0.4.0`.
3. The release workflow builds, runs the tests, publishes the single exe with a SHA-256 file and creates a **draft** release. Check it, then publish it on GitHub.

The exe is not code signed yet. Signing (Azure Trusted Signing or an OV certificate) fits in the release workflow between publish and upload.
