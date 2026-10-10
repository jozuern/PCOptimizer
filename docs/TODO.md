# Open items

Most items are left over from the code audit of 2026-10-10 (version 0.4.0); every other finding of that audit was checked against the code at version 0.4.7 and is fixed.

- **Repair steps show no result:** `StepRow.Ok` (`src/Optimizer.App/ViewModels/PageViewModels2.cs`) is never bound, so a failed Windows Update repair step looks like a successful one; only its detail text differs. Show an OK or failed icon per step.
- **DNS benchmark does not mark the fastest server:** `DnsResultRow.IsBest` (`src/Optimizer.App/ViewModels/PageViewModels.cs`) is never bound. Highlight that row or remove the property.
- **DNS change without a timeout:** `DnsAction` sets the servers through a `ManagementObjectSearcher` query without a timeout (`src/Optimizer.Core/Actions/ExtendedActions.cs`); a hung WMI service blocks the change.
- **Runtime tweak ids without `TweakIds.Slug`:** `fix.refreshRate.*` and `fix.ethernetAuto.*` in `src/Optimizer.Core/Findings/Checks/FixChecks.cs` build their ids by hand.
- **`privacy.inkingTypingOff` stays Preview:** in the VM, "Custom inking and typing dictionary" in Settings stayed on although both `RestrictImplicit*Collection` policies were set ([Preview review](vm-test-results-2026-10-10.md#preview-review)). Find out whether it needs a restart, another value or a different description.
- **`gpu.gameDvrOff` stays Preview:** the VM detected it as Partial before and after apply. Find the value Windows does not take.
- **Possible midnight flake:** `tests/Optimizer.Core.Tests/TestData.cs` uses `DateTime.Today`.
- **MainViewModel is too large:** about 1300 lines (navigation, scan, profiles, score, changes, banners, updates, language). Split it into page and service view models and add an App test project for `ChangeRunner` and the row state logic.
