# Development

Notes for working on PCOptimizer. For what the app does, see [README.md](README.md).

## Contributions

PCOptimizer is source-available under the [PolyForm Strict License 1.0.0](LICENSE), not open source: the license does not allow changed versions or redistribution. Bug reports and ideas are welcome as issues. Code contributions only by prior agreement with the author; please open an issue first. Security problems: see [SECURITY.md](SECURITY.md).

## Build and run

```powershell
dotnet build                     # Debug: asInvoker manifest, runs without UAC (TPM and some counters show "Unknown")
dotnet test                      # unit tests + explanation lint, also the read-only checks of this PC (Category=Hardware)
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
| `--scroll end` or `--scroll <pixels>` | scrolls the page to the end, or down by that many pixels, before the screenshot (for example Settings > About) |
| `--select <id>` | opens a finding or tweak in the details pane (e.g. `A.rebar`) |
| `--category <key>` | Tweaks page filtered to one category before the screenshot (e.g. `Privacy`) |
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

Data: `%ProgramData%\PCOptimizer` when elevated; Debug runs without admin rights use `%LocalAppData%\PCOptimizer` instead. Before the first log or settings access, `Program.Main` secures the elevated folder: a link is removed, a folder not created by an administrator is deleted, files inside that Administrators or SYSTEM do not own are removed, and the rest is locked to Administrators and SYSTEM. Contents: `backups\` (originals per change), `exports\` (BCD and power plan exports), `logs\`, `tools\` (PresentMon and captures), `runtime\` (the native WPF libraries the single-file exe extracts: it restarts itself once with `DOTNET_BUNDLE_EXTRACT_BASE_DIR` pointing here instead of `%TEMP%`), `updates\`, `settings.json` (language, theme, Expert mode, profile, update check), `removed-apps.json`, `throttle.json`.

Native libraries: `NativeLibraryGuard` resolves P/Invoke targets to System32 or the protected runtime folder and refuses a DLL of that name next to the exe. New P/Invokes need nothing extra; never load a DLL by a path a standard user can write to.

Catalog data in `src/Optimizer.Core/Catalog/`: `Tweaks/*.json` (tweaks), `Data/profiles.json` (profiles: per-profile impact of tweaks and findings, recommendations, what works against each), `Data/labels.json` (generated text, EN and DE), `Docs/{en,de}/*.md` (one explanation page per tweak and check). Entries marked `verified: false` (one anti-cheat, some service names) say so on their explanation page. Tweaks the evidence does not support are listed in [docs/not-included.md](docs/not-included.md) instead of the catalog.

## Testing changes safely

- `dotnet test` runs the engine against a registry sandbox under `HKCU\Software\PCOptimizerTest` and fake system APIs. Nothing on the real system changes.
- `--filter Category=Hardware` runs read-only checks of the real adapters on this PC (power, services, tasks, displays, NVAPI, startup scan, signatures, AppX list, cleanup sizes, drive health, counters, sensors).
- Real apply and undo round trips belong in a Hyper-V VM with checkpoints: follow [docs/vm-test-plan.md](docs/vm-test-plan.md) (the last run: [docs/vm-test-results-2026-10-10.md](docs/vm-test-results-2026-10-10.md)). On a real PC, start with a harmless reversible tweak such as "Show file extensions", then undo it on the Changes page.
- Not yet checked on real hardware: NVIDIA driver settings and network adapter properties (section 5 of the VM test plan), the laptop profiles and battery checks (tested with simulated laptops only), battery capacity readings, Wi-Fi band, AMD-specific checks.

## Adding a tweak

1. Catalog entry in `Catalog/Tweaks/*.json`: a documented setting (Microsoft or the hardware vendor), or an undocumented value under the rules in [Undocumented values](#undocumented-values); correct risk, `restart`/`signOut`, `bootCritical`, `antiCheatSensitive`, impact 0 to 5 with basis `situational` or `disputed` (`measured` only with a cited measurement), sources you opened. Set `"preview": true` when it is risky and not yet tested on real Windows; Expert and boot-critical tweaks must be previews (`CatalogRuleTests`).
2. EN and DE explanation pages following [docs/explanation-style-guide.md](docs/explanation-style-guide.md); the docs lint checks the structure, summary length, sources, typography and banned words.
3. A sandbox test for apply and undo (see `UndoTests` and `EngineTests`).
4. Regenerate the VM test plan tables: run `dotnet test` once with the environment variable `PCO_UPDATE_DOCS=1`. The counts in README.md are checked against the catalog (`DocsConsistencyTests`).
5. A tweak that is removed later stays undoable: undo uses the action stored with each backup entry.

## Undocumented values

A value Microsoft or the vendor does not document is allowed only when all of this holds; the docs lint (`CheckUndocumented`) enforces the first three:

- The change is harmless and fully reversible, below Expert risk.
- The tweak has `"undocumented": true` (the app shows an "Undocumented value" badge), and both explanation pages say that Microsoft does not document the value.
- It is proven to work: `"proof"` names the test or source that showed the effect (a VM test result that checked the visible effect, not only the registry value). Without proof the tweak is `"preview": true`; the VM test plan lists what to check.
- It is not something risky that nobody documents (for example NVIDIA Ultra Low Latency through undocumented driver values, or forced Resizable BAR). Those stay in [docs/not-included.md](docs/not-included.md).

## New dependencies

Every NuGet package that ships in the exe needs its license text in `src/Optimizer.App/Licenses/` and an entry in `licenses.json`. The test `EveryShippedPackageHasItsLicense` fails until it is there. Add it to `docs/THIRD-PARTY-NOTICES.md` as well.

## Releases

Every commit raises the patch version (0.3.0 to 0.3.1) through the pre-commit hook in `.githooks`. Enable it once per clone with `git config core.hooksPath .githooks`. Change `<Version>` by hand for a new minor or major version; the hook keeps a version changed in the same commit. Skip it for one commit (for example `--amend`) with `SKIP_VERSION_BUMP=1 git commit`.

1. Pick the commit to release; its `<Version>` in `src/Optimizer.App/Optimizer.App.csproj` is the release version. The tag must match it, or the workflow stops.
2. Tag and push: `git tag v0.4.0` and `git push origin v0.4.0`.
3. The release workflow builds, runs the tests, publishes the single exe with a SHA-256 file and a signature file (`PCOptimizer.exe.sig`) and creates a **draft** release. Check it, then publish it on GitHub.

The in-app update installs a release only when its signature matches the public key in `src/Optimizer.Core/Updates/update-key.pem`. The release workflow signs with the private key from the repository secret `UPDATE_SIGNING_KEY` and stops when the secret is missing. Create the key pair once with `scripts/new-update-key.ps1`; the script says how to store the private key. Replace it only when it is lost or leaked: versions with the old public key then reject new releases, and users have to download the next version by hand.

The exe is not code signed yet. Signing (Azure Trusted Signing or an OV certificate) fits in the release workflow between publish and upload.
