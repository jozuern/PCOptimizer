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

The run of 2026-10-11 ([results](vm-test-results-2026-10-11.md)) proved 28 previews and the visible effect of `gpu.gameMode`, confirmed the elevation boundary (H4, L-P4) and fixed the DISM umlauts. Still open:

- **`privacy.locationOff` has no effect:** Location services stays on and apps keep their access, also after a restart. Find the value behind the Settings switch on 26H2 ([F1](vm-test-results-2026-10-11.md#f1-privacylocationoff-has-no-effect)).
- **`focus.lockScreenTipsOff` turns off Windows spotlight:** check whether writing only `SubscribedContent-338387Enabled` avoids it ([F2](vm-test-results-2026-10-11.md#f2-focuslockscreentipsoff-turns-off-windows-spotlight)).
- **`privacy.inkingTypingOff` stays Preview:** in the VM, "Custom inking and typing dictionary" in Settings stayed on although both `RestrictImplicit*Collection` policies were set ([Preview review](vm-test-results-2026-10-10.md#preview-review)). Find out whether it needs a restart, another value or a different description.
- **`gpu.gameDvrOff` stays Preview:** the VM detected it as Partial before and after apply. Find the value Windows does not take.
- **Need real hardware or traffic capture:** `gpu.windowedOptimizations` (discrete GPU), `display.autoHdrOn` and `display.vrrOn` (HDR or VRR display), `network.dohAutoUpgrade` (Settings kept showing "Unencrypted" while Windows reported auto-upgrade on).
- **Storage analysis before the first scan:** the refusal ("Wait until the PC scan has finished") could not be timed through UI Automation and is not confirmed.
- **V-Cache driver (second audit M22):** the service name `amd3dvcacheSvc` needs a check on an X3D PC.
