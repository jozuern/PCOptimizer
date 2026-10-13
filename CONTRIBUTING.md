# Development

Notes for working on Tunero. For what the app does, see [README.md](README.md).

## Contributions

Tunero is open source under the [GNU General Public License version 3](LICENSE) (GPL-3.0-only). Bug reports and ideas are welcome as issues. For a code change, please open an issue first so the approach can be agreed on; a pull request is accepted under the same license and reviewed by the maintainer before it is merged (team roles: README, [Code signing](README.md#code-signing)). Code from other projects only under a license compatible with GPL-3.0-only, with its notice. Security problems: see [SECURITY.md](SECURITY.md).

## Build and run

```powershell
dotnet build                     # Debug: asInvoker manifest, runs without UAC (TPM and some counters show "Unknown")
dotnet test                      # unit tests + explanation lint, also the read-only checks of this PC (Category=Hardware)
dotnet build -c Release && dotnet test -c Release --no-build --filter "Category!=Hardware"   # what CI runs (the build also compiles the app and its XAML)
dotnet publish src/Optimizer.App -p:PublishProfile=SingleFile   # writes artifacts/publish/Tunero.exe
```

Developer switches (all read-only). Runs with `--screenshot` or `--perf` open the window and its dialogs below the screen,
without focus or taskbar button, so they can run while you work; the PNGs are rendered from the window, not the screen.
In an elevated run (the published exe) the files they write are only created new, never overwritten, and `--expert on`
is ignored: whoever starts the exe names the paths, and the app writes with administrator rights.

| Switch | Effect |
|---|---|
| `--report <file.md>` | headless scan, writes all findings with explanations and the system info as Markdown |
| `--screenshot <file.png>` | opens the UI, scans, renders the window to PNG and exits |
| `--page <name>` | page for `--screenshot`: Overview, Tweaks, Advisor (Manual checks), Network, Debloat (Remove apps), Cleanup, Startup, Services, Apps, Tools, Health, Changes, Hardware (System info), Settings |
| `--shot-scanning <file.png>` | renders the window while the first scan still runs (the Overview says the scan is reading the PC) |
| `--pane closed` | collapses the navigation rail before the screenshot |
| `--width <pixels>` | window width for the screenshot (for example 1040, the minimum, with `--select`) |
| `--height <pixels>` | window height for the screenshot (below 900 the pane drops its group headings, below 790 it folds to the rail; the window is at least 720 high) |
| `--search <text>` | types into the search box (Ctrl+F) before the screenshot; the results popup is saved to `<file>-search.png` |
| `--scroll end` or `--scroll <pixels>` | scrolls the page to the end, or down by that many pixels, before the screenshot (for example Settings > About) |
| `--select <id>` | opens a finding or tweak in the details pane (e.g. `A.rebar`) |
| `--category <key>` | opens that category group on the Tweaks page before the screenshot (a section such as `privacy`, or a catalog category such as `Browser`); `--select` opens the group of the selected tweak |
| `--sensors on\|all` | switches the live sensor readings on (Health page, read only) and waits for the first values; `all` also opens every component's full list |
| `--lang en\|de`, `--theme System\|Dark\|Light` | override saved preferences |
| `--confirm <tweak id> --confirm-shot <file.png>` | with `--screenshot`: renders the confirmation dialog of a tweak (nothing is applied) |
| `--message-shot <file.png>` | with `--screenshot`: renders the question dialog with two buttons (update) to the file and with three (restore point) to `<file>-3.png` |
| `--licenses-shot <file.png>` | with `--screenshot`: renders the Licenses window |
| `--switch-theme Light\|Dark` | switches the theme while the page is open, before the screenshot (finds text that keeps the old colors, and switches that keep the old accent) |
| `--perf <file.txt>` | times the start (steps after the process start, scan steps, the tools it ran), page switches, filters, profile, Expert mode, language and theme switches and a full rebuild until the UI is idle; saves nothing, writes the result and exits |
| `--profile <id>` | start with a profile: gaming, laptopGaming, battery, office, quiet, lowEnd (saved only when changed in the app) |
| `--preview-drift on` | shows the "changes reset" banner with sample entries (nothing is read or changed) |
| `--expert on` | Expert mode for this session (runs without admin rights only) |

## Layout

```
src/Optimizer.Core/   scanner, findings, engine, actions, catalog data (Catalog/Data/*.json), explanation pages (Catalog/Docs/{en,de}/*.md),
                      debloat, cleanup, startup, services, apps, tools (storage, features, update repair, PresentMon, sensors), release check
src/Optimizer.App/    WPF UI (WPF-UI 4.3, CommunityToolkit.Mvvm, Markdig): one page per area, shared change runner;
                      Licenses/ holds every license text that ships in the exe (licenses.json lists them)
tests/                xUnit: rule tests with mocked hardware, engine tests in a registry sandbox, docs lint, read-only hardware checks (Category=Hardware)
docs/                 privacy, third-party notices, VM test plan, explanation style guide, notes from real hardware, brand (icon, logo, colors)
website/              tunero.dev (Astro, static): reads the counts, catalog and explanation pages at build time; rules in website/DESIGN.md
.github/workflows/    CI (build, tests without Category=Hardware, single-exe artifact), the tag-triggered release and the website
```

Website: `cd website`, `npm ci`, then `npx astro dev` (preview) or `npx astro build` (output in `website/dist`). `pages.yml` builds and publishes it to GitHub Pages on pushes to main that touch the site, the README or the catalog, and after each release. Install commands appear only for package managers whose `live` flag is set in `website/src/lib/data.ts`. The docs lint checks its copy (`website/src`) for banned typography.

Data: `%ProgramData%\Tunero` when elevated; Debug runs without admin rights use `%LocalAppData%\Tunero` instead. Before the first log or settings access, `Program.Main` secures the elevated folder: a link is removed, a folder not created by an administrator is deleted, files inside that Administrators or SYSTEM do not own are removed, and the rest is locked to Administrators and SYSTEM. Contents: `backups\` (originals per change; an account's own settings under `users\<SID>`, undone changes in `history\`, undo that waits for a restart in `pending-undo\`), `exports\` (BCD and power plan exports), `logs\`, `tools\` (PresentMon; its capture files are deleted once read), `temp\` (TEMP for the tools the app starts, emptied at every start), `runtime\` (the native WPF libraries the single-file exe extracts: it restarts itself once with `DOTNET_BUNDLE_EXTRACT_BASE_DIR` pointing here instead of `%TEMP%`), `updates\`, `settings.json` (language, theme, Expert mode, profile, update check), `removed-apps.json`, `throttle.json`, `startup-snapshot.json`, `tcpip-reset.log`.

Native libraries: `NativeLibraryGuard` resolves P/Invoke targets to System32 or the protected runtime folder and refuses a DLL of that name next to the exe. New P/Invokes need nothing extra; never load a DLL by a path a standard user can write to.

Catalog data in `src/Optimizer.Core/Catalog/`: `Tweaks/*.json` (tweaks), `Data/profiles.json` (profiles: per-profile impact of tweaks and findings, recommendations, what works against each), `Data/labels.json` (generated text, EN and DE), `Docs/{en,de}/*.md` (one explanation page per tweak and check). Entries marked `verified: false` (one anti-cheat, some service names) say so on their explanation page. Tweaks the evidence does not support are listed in [docs/not-included.md](docs/not-included.md) instead of the catalog.

## App design

The app is WPF with WPF-UI and nothing else (no WinForms, WebView2 or WinUI inside it), in the Windows 11 Fluent vocabulary: theme brushes and the Windows accent, Segoe UI Variable, spacing in steps of 4, light and dark. One element carries the app's own look, the **rating mark** (`Views/RatingMark.cs`): a tweak's or check's impact 0 to 5 as a light numeral and five bars, outlined when the evidence is disputed, nothing for 0. It is the same scale as the rating board on tunero.dev.

- The light weight belongs to what the scan says: the rating marks and the Overview's verdict sentence. Headings and text keep the regular Fluent weights.
- A row's impact is its mark, in one column beside the switch or button; the row's grey line names what it affects, and the details pane puts the mark next to "Impact 2 of 5".
- Lists keep their columns: switches, marks and buttons have fixed places on every row, so a list can be read down a column.
- Check a UI change with `--screenshot` in both themes and languages and at 1040 px with the details pane open, and with `--perf` before and after.

## Testing changes safely

- `dotnet test` runs the engine against a registry sandbox under `HKCU\Software\TuneroTest` and fake system APIs. Nothing on the real system changes.
- Run the tests once from an elevated terminal before changing security code: the data folder lock (`SecureFolder`) is only exercised elevated, as on the CI runners; a normal terminal checks only that it fails safely.
- `--filter Category=Hardware` runs read-only checks of the real adapters on this PC (power, services, tasks, displays, NVAPI, startup scan, signatures, AppX list, cleanup sizes, drive health, counters, sensors). They reach the engine's adapters through `ReadOnlyContext.Create` (tests project), whose write methods throw and whose tool runner refuses commands that look like a change (a list of write switches and verbs, not a proof); `ArchitectureTests` fails when a test builds a real adapter or calls `Process.Start` or `StreamingProcess.RunAsync` without being listed there. The hardware scanner and some probes start their read tools themselves (wevtutil, the task scheduler), outside that runner: a new probe must only read.
- Real apply and undo round trips belong in a Hyper-V VM with checkpoints: follow [docs/vm-test-plan.md](docs/vm-test-plan.md). On a real PC, start with a harmless reversible tweak such as "Show file extensions", then undo it on the Changes page.
- Not yet checked on real hardware: NVIDIA driver settings and network adapter properties (section 5 of the VM test plan), the laptop profiles and battery checks (tested with simulated laptops only), battery capacity readings, Wi-Fi band, AMD-specific checks.

## Adding a tweak

1. Catalog entry in `Catalog/Tweaks/*.json`: a documented setting (Microsoft or the hardware vendor), or an undocumented value under the rules in [Undocumented values](#undocumented-values); correct risk, `restart`/`signOut`, `bootCritical`, `antiCheatSensitive`, impact 0 to 5 with basis `situational` or `disputed` (`measured` only with a cited measurement), sources you opened. Set `"preview": true` when it is risky and not yet tested on real Windows; Expert and boot-critical tweaks must be previews (`CatalogRuleTests`).
2. EN and DE explanation pages following [docs/explanation-style-guide.md](docs/explanation-style-guide.md); the docs lint checks the structure, summary length, sources and typography.
3. A sandbox test for apply and undo (see `UndoTests` and `EngineTests`).
4. Regenerate the VM test plan tables: run `dotnet test` once with the environment variable `PCO_UPDATE_DOCS=1`. The counts in README.md are checked against the catalog (`DocsConsistencyTests`).
5. A tweak that is removed later stays undoable: undo uses the action stored with each backup entry.

A tweak left out goes into [docs/not-included.md](docs/not-included.md) with its reason and sources, and into `Catalog/Data/notincluded.json` with a short reason in EN and DE and the words people search for, so the app's search explains it instead of finding nothing. `NotIncludedEntriesMatchTheDocument` checks that both lists have the same entries.

### Backup files are a promise

Undo has to work on every backup file an earlier release wrote, also after an update. Action type names (`[JsonDerivedType]` on `TweakAction`), action property names, target keys (`reg:`, `pwr:`, `nic:` and so on) and the encodings of stored values (`missingKeys:N`, `scheme-created`, `raw:<hex>`) are frozen: add, never rename or remove. `BackupFormatTests` restores a saved backup of every action type from `tests/Optimizer.Core.Tests/Fixtures/backups` and fails when one no longer undoes. A new action type needs a scenario there; run the tests once with `TUNERO_WRITE_BACKUP_FIXTURES=1` to write its file, and commit the new file without touching the others. A change older versions would misread raises `BackupFormat.Current`; the app then refuses files of a newer format instead of guessing.

## Undocumented values

A value Microsoft or the vendor does not document is allowed only when all of this holds; the docs lint (`CheckUndocumented`) enforces the first three:

- The change is harmless and fully reversible, below Expert risk.
- The tweak has `"undocumented": true` (the app shows an "Undocumented value" badge), and both explanation pages say that Microsoft does not document the value.
- It is proven to work: `"proof"` names the test or source that showed the effect (a VM test result that checked the visible effect, not only the registry value). A results file that is no longer in `docs/` is cited with the commit that has it ("docs/vm-test-results-2026-10-11.md in git 60c3a93^"); `ProofFilesExist` checks every path. Without proof the tweak is `"preview": true`; the VM test plan lists what to check.
- Several unofficial sources that document and prove a value count like an official source, for tweaks and for every other feature: they must be independent of each other (copies of one post count once) and show it working (a test, a screenshot, reverse engineering), not only repeat it. That counts as proof; the value still gets `"undocumented": true` and the sentence on both pages when Microsoft or the vendor does not document it, and the pages cite those sources.
- It is not something risky that nobody documents (for example NVIDIA Ultra Low Latency through undocumented driver values, or forced Resizable BAR). Those stay in [docs/not-included.md](docs/not-included.md).

## New dependencies

Every NuGet package that ships in the exe needs its license text in `src/Optimizer.App/Licenses/` and an entry in `licenses.json`. The test `EveryShippedPackageHasItsLicense` fails until it is there. Add it to `docs/THIRD-PARTY-NOTICES.md` as well.

## Releases

Every commit raises the patch version (0.3.0 to 0.3.1) through the pre-commit hook in `.githooks`, a merge commit above both of its sides. A merge without conflicts stops before its commit (`pre-merge-commit`: git would not take a version raised there into the merge commit); finish it with `git commit --no-edit`, which raises the version. After a commit of named paths only (`git commit -- file`), `post-commit` sets the csproj in the index to the committed version. CI checks every commit of a push against each of its parents. Enable it once per clone with `git config core.hooksPath .githooks`. Change `<Version>` by hand for a new minor or major version; the hook keeps a version changed in the same commit. Skip it for one commit (for example `--amend`) with `SKIP_VERSION_BUMP=1 git commit`.

1. Pick the commit to release; its `<Version>` in `src/Optimizer.App/Optimizer.App.csproj` is the release version. The tag must match it, or the workflow stops. Rename `## Unreleased` in CHANGELOG.md to that version first (`## 1.0.0`): the release notes are that section, and the workflow stops without it.
2. Tag and push: `git tag v0.4.0` and `git push origin v0.4.0`.
3. The release workflow builds, runs the tests and publishes the single exe. The release job then waits for your approval (Actions > the run > Review deployments), because it uses the `release` environment. Once SignPath is set up (below), it sends the exe to SignPath and waits until you approve the signing request there. Then it adds a SHA-256 file and a signature file (`Tunero.exe.sig`), both of the signed exe, and creates a **draft** release. Check it, then publish it on GitHub.
4. winget: publishing the release starts the workflow `winget`, which submits the manifest to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) with Microsoft's `wingetcreate` (a pull request "Update: jozuern.Tunero to x.y.z" from your fork); a volunteer moderator merges it. It needs the secret `WINGET_CREATE_GITHUB_TOKEN` of the environment `winget` (only `v*.*.*` tags): a classic personal access token with only the `public_repo` scope, because `wingetcreate` supports no fine-grained token. Renew it before it expires. Before it submits, the workflow checks that the release's `Tunero.exe` matches its `.sha256` and that the checksum carries the app's update signature for this version, so an asset replaced after publishing never reaches winget. To submit a release again, run the workflow by hand from its tag (Actions > winget > Run workflow). `pwsh scripts/winget-manifest.ps1 -Version x.y.z` writes and validates the same manifest locally, for a pull request by hand or a test with `Tools/SandboxTest.ps1` from winget-pkgs in Windows Sandbox. The exe needs administrator rights, so the pipeline's start test as a standard user can fail (`Validation-Executable-Error`): say so in the pull request. While `jozuern.Tunero` is not in winget-pkgs yet, the workflow only notes that in its summary: the first version goes in by hand (`scripts/winget-manifest.ps1`, then `wingetcreate submit <folder>`).
5. Chocolatey: publishing the release also starts the workflow `chocolatey`, which checks the release the same way, builds the package `tunero` with `scripts/chocolatey-package.ps1` (an install script that downloads `Tunero.exe` from the release and checks its SHA-256, no embedded binary) and pushes it to the [community repository](https://community.chocolatey.org/packages/tunero). It needs the secret `CHOCOLATEY_API_KEY` of the environment `chocolatey` (only `v*.*.*` tags), from chocolatey.org > Account > API Keys. Every version passes Chocolatey's automatic checks (validator, verifier, VirusTotal); the first one is also reviewed by a volunteer moderator, which can take a while. Until it is approved, Chocolatey refuses every newer version (403): the workflow then says so in its summary and passes, and after the approval you run it by hand for the newest tag. A version number can be pushed only once: fix a package with a fourth segment (`0.8.0.20261013`, `choco pack` after editing the version in the written nuspec). The package downloads from `github.com/jozuern/Tunero`; when that address does not answer, the workflow pushes nothing and says so.
6. Scoop: the bucket [jozuern/scoop-bucket](https://github.com/jozuern/scoop-bucket) (made from Scoop's bucket template) updates itself: its workflow "Tunero update" runs every few hours and writes version and hash to bucket/tunero.json only when the release's Tunero.exe matches its .sha256 and carries the update signature, checked with a copy of update-key.pem pinned in the bucket (keys/tunero-update-key.pem; replace it there too if the key is ever replaced). Nothing to do per release. `scripts/scoop-manifest.ps1 -Version x.y.z` writes the manifest for a fix by hand; it has no `checkver` or `autoupdate`, because Scoop's Excavator would take the `.sha256` file unchecked. The hand-run package scripts (`scoop-manifest.ps1`, `chocolatey-package.ps1`, `winget-manifest.ps1`) take the hash from `scripts/verify-release.ps1`, which checks the signature the same way. The [Extras bucket](https://github.com/ScoopInstaller/Extras) takes apps with at least 100 GitHub stars or 50 forks; until then the own bucket is the way.

A copy installed by a package manager does not replace itself: "Update now" hands the update to it after the app has ended (`PackageInstall`). winget and a Scoop install in the user's profile run as the signed-in user; Chocolatey runs elevated, only when `choco.exe` and its folder can be changed by administrators alone and no folder above lets others rename or replace what is in it (`C:\ProgramData` only lets users add folders; a Chocolatey in a user's folder, as UniGetUI installs it, gets the command to type instead). A global Scoop install (`--global`) is never run from the elevated app, because Scoop's own scripts sit in the user's profile: the app shows `scoop update tunero --global` instead. Settings > Uninstall removes such a copy with its package manager and the data folder with the app's own cleanup. A version reaches winget and Chocolatey only after their review; until then "Update now" shows the package manager's message (for Chocolatey and Scoop, which report no error then, the window compares the exe's version with the expected one) and starts the old version again.

The in-app update installs a release only when its signature matches the public key in `src/Optimizer.Core/Updates/update-key.pem`. The release workflow signs with the private key from the secret `UPDATE_SIGNING_KEY` of the GitHub environment `release` and stops when the secret is missing or does not match that public key. Keep the secret in that environment, not in the repository (a repository secret is readable by a workflow on any pushed branch or tag), and let the environment allow only `v*.*.*` tags and require your approval (both are set up). The repository rulesets "Release tags" (only administrators create, move or delete `v*` tags) and "Protect main" (no deletion, no force push) cover the rest. Create the key pair once with `scripts/new-update-key.ps1`; the script says how to store the private key. Every version also carries a backup key (`update-key-backup.pem`, made with `scripts/new-update-key.ps1 -Backup -PrivateKeyFile <path on a USB stick>`): its private half lives only offline and is never used while the release key works. If the release key is lost or leaked, store the backup key as `UPDATE_SIGNING_KEY`, release (every installed copy accepts it, and the workflows check against both keys), then make a new backup key. Only a version that already carries a key can accept it, so both are in every release, and the Scoop bucket's pinned copy (`keys/`) needs the backup key too.

### The rename to Tunero (once)

The app was called PCOptimizer up to 0.7.3. The order matters for the in-app update of installed copies:

1. Release the first Tunero version (0.8.0) while the repository is still named `jozuern/PCOptimizer`. Versions up to 0.7.3 accept only download links under that name and look for `PCOptimizer.exe`, which 0.8.0 carried next to `Tunero.exe` (same file, same signature); after the rename GitHub hands out links under the new name, so those versions open the release page instead. They see the release when they start and check for updates. Rename soon after publishing all the same: the links in 0.8.0 (bug and error reports, release page) already use the new name.
2. Then rename the repository on GitHub (Settings > General > Repository name: `Tunero`). GitHub redirects the old address. Versions from 0.8.0 on ask for releases by the repository's id (`ReleaseCheck.RepositoryId`) and accept links under both names, so they keep updating; versions up to 0.7.3 that did not update before the rename open the release page instead.
3. winget: the first submission (pull request #450585, `jozuern.PCOptimizer` 0.6.4) is replaced by `jozuern.Tunero` 0.8.0 from `scripts/winget-manifest.ps1`. The `winget` workflow (`wingetcreate update`) takes over once that package is merged.
4. Chocolatey and Scoop download from `github.com/jozuern/Tunero`, which exists only after the rename: run the `chocolatey` workflow from the tag after the rename (its automatic run on publishing pushes nothing before it), and add the first manifest to the Scoop bucket after the rename.
5. Releases after 0.8.0 carry only `Tunero.exe`. Keep the legacy names in the code (`LegacyFolderName`, `StartMenuShortcut.LegacyFileNames`, `FormerNames`, `LegacyOwnProfileName`, `LegacyRepository`, `LegacyExeAsset`, `WingetPackage.LegacyId`) for longer: they cost nothing and keep old installs working.

### Code signing (SignPath Foundation)

The exe is not code signed yet. [SignPath Foundation](https://signpath.org/), which signs open source projects for free with its own certificate, declined the first application on 2026-10-13 for lack of public visibility (stars, forks, contributors, articles, discussions) and welcomes a new one once the project is better known; a paid SignPath subscription would be the other way. The release workflow keeps the step; it runs only when the repository variable `SIGNPATH_ORGANIZATION_ID` is set. After SignPath accepts the project:

1. In SignPath, link the predefined trusted build system "GitHub.com" to the project and install the SignPath GitHub App for this repository.
2. Create the project with the slug `Tunero`, paste [.signpath/artifact-configuration.xml](.signpath/artifact-configuration.xml) as its artifact configuration, and create the signing policy `release-signing` with yourself as approver (every request needs a manual approval).
3. Create a CI user with submitter rights for that policy. Store its API token as the secret `SIGNPATH_API_TOKEN` of the GitHub environment `release` (not as a repository secret) and the organization ID as the repository variable `SIGNPATH_ORGANIZATION_ID`.
4. Change the code signing section in the README to the current state, with SignPath's attribution ("Free code signing provided by SignPath.io, certificate by SignPath Foundation").

SignPath signs only an exe built on GitHub's runners in this workflow run whose version resource says product "Tunero" and the tag's version (`IncludeSourceRevisionInInformationalVersion` is off for that reason). The signing request waits up to four hours for the approval. The policy in the README ([Code signing](README.md#code-signing)) lists the team roles SignPath requires; update it when someone joins.
