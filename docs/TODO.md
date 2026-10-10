# Open items

Left over from the code audits of 2026-10-10 (the first one at version 0.4.0, the [second](audit-2026-10-10-2.md) and the [third](audit-2026-10-10-3.md)); every other finding was checked against the code and is fixed. The third audit's report lists why each remaining item is open.

## Code

- **Repair steps show no result:** `StepRow.Ok` (`src/Optimizer.App/ViewModels/PageViewModels2.cs`) is never bound in `ToolsPage.xaml`, so a failed Windows Update repair step looks like a successful one; only its detail text differs. Show an OK or failed icon per step.
- **DNS change without a timeout:** `SystemNetworkManager.SetDns` (`src/Optimizer.Core/Actions/ExtendedActions.cs`) runs a `ManagementObjectSearcher` query without a timeout; a hung WMI service blocks the change.
- **Runtime tweak ids without `TweakIds.Slug`:** `fix.refreshRate.*` and `fix.ethernetAuto.*` in `src/Optimizer.Core/Findings/Checks/FixChecks.cs` build their ids by hand.
- **Per-user shell extensions (third audit M7):** the startup scanner reads shell extensions only from HKLM; per-user handlers and per-user CLSID overrides under `HKCU\Software\Classes` are not shown.
- **Raw error text (third audit L-A7):** the scan status shows the exception message, and the Changes page log shows English engine lines in the German UI. Needs a typed tool exception that the UI maps to localized text.
- **Version check of multi-commit pushes (third audit L-B2):** CI checks only the last commit of a push.
- **Runtime license version (second audit L21, third audit L-B4):** `EveryShippedPackageHasItsLicense` reads the test host's runtime version instead of the one the publish ships.
- **Catalog round trip (third audit L-B9):** `CatalogRoundTripTests` covers registry and accessibility actions only; power, service, BCD, DNS, task and NVIDIA actions have no apply and undo round trip.
- **Possible midnight flake:** `tests/Optimizer.Core.Tests/TestData.cs` uses `DateTime.Today`.
- **Large files (third audit, refactors 1 to 5):** `MainViewModel.cs` has about 1350 lines (navigation, scan, profiles, score, changes, banners, updates, language). Split it into page and service view models behind an `IPageHost` interface and add an App test project for `ChangeRunner` and the row state logic. The report also proposes one process-execution layer, one startup trust model, shared JSON and registry helpers, and an analyzer baseline.

## Checks in the VM

- **Previews to prove:** `gpu.windowedOptimizations`, `explorer.endTask`, `privacy.locationOff` and `network.throttlingIndex` (marked undocumented by the third audit), the previews added after the first VM run, and the visible effect of `gpu.gameMode` (its current proof checks only the values).
- **`privacy.inkingTypingOff` stays Preview:** in the VM, "Custom inking and typing dictionary" in Settings stayed on although both `RestrictImplicit*Collection` policies were set ([Preview review](vm-test-results-2026-10-10.md#preview-review)). Find out whether it needs a restart, another value or a different description.
- **`gpu.gameDvrOff` stays Preview:** the VM detected it as Partial before and after apply. Find the value Windows does not take.
- **Elevation boundary:** confirm that an elevated start with `DOTNET_DiagnosticPorts` or `DOTNET_EnableEventPipe` in the user's environment refuses to run, and whether the elevated app honours per-user COM registrations of `WScript.Shell` and `Schedule.Service`.
- **German Windows:** the live DISM output on the Health page shows umlauts correctly.
- **V-Cache driver (second audit M22):** the service name `amd3dvcacheSvc` needs a check on an X3D PC.
