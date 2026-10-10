# VM test results, 2026-10-10

Run of [vm-test-plan.md](vm-test-plan.md) sections 2, 3 and 4 at commit 20650b3 (version 0.4.0). Sections 1, 5 and 6 are still open (see [Not covered](#not-covered)).

## Setup

- Hyper-V VM `PCO-Test`: generation 2, 4 virtual processors, Secure Boot, virtual TPM, nested virtualization on, 80 GB disk. 8 GB memory for section 4, 16 GB for sections 2 and 3.
- Windows 11 Pro 26H2 (English, unmodified Microsoft ISO), local administrator account, Steam installed, not activated. Section 4 ran on build 26300.9550; sections 2 and 3 and the retest ran on 26300.9457 after a restore of the `clean` checkpoint, which predates the last cumulative update.
- Every tweak ran alone from the `clean` checkpoint state, through a console runner that builds the real `TweakEngine` the way `AppServices` does (real adapters, secured backup store in `%ProgramData%\PCOptimizer`) and calls `ApplyAsync` and `Revert`. The runner refuses to start outside a Hyper-V VM. The UI (`ChangeRunner`, dialogs, pages) was not part of this run.
- Each change was checked independently of the engine, over PowerShell Direct: registry values read directly, service `Start` and `DelayedAutostart`, `powercfg /q` and `/getactivescheme`, `bcdedit /enum {current}`, `Get-ScheduledTask`, `Get-MMAgent`, the power mode overlay values, and `Win32_DeviceGuard` for VBS.
- Steps per tweak: read the real values, apply, read again, check that the engine reads every desired value, restart where the plan says restart or sign out, check that nothing was lost, undo, read again and compare with the first read. Section 2 also restarted after undo.

## Summary

| Result | Tweaks |
|---|---|
| OK | 34 |
| OK, with a difference after undo that does not change behavior (see N1 to N3) | 22 |
| Already set on a clean Windows, apply reports nothing to do | 6 |
| Bug found (B1 to B4) | 6 |
| Not applicable in this VM | 7 |
| Could not be tested in Hyper-V before the B5 fix | 4 |
| Needs real hardware (section 5) | 7 |

## Bugs

### B1. Windows defaults count as conflicting tweaks

`TweakEngine.Preflight` (`src/Optimizer.Core/Tweaks/TweakEngine.cs:179`) blocks a tweak when a tweak in its `conflictsWith` list is in the applied set. The applied set comes from detection, so a Windows default that matches a tweak counts as applied even though the app never changed it:

- Balanced is the default plan, so `power.balancedPlan` is detected as applied, and `power.gamingPlan` and `power.ultimatePlan` are blocked with "Conflicts with an active tweak: power.balancedPlan".
- The default processor boost mode makes `power.turboRestore` detect as applied, and `quiet.boostOff` is blocked. On a stock PC it can never be applied.

The block cannot be overridden. With Power saver active (so Balanced is not detected), both plans passed the full round trip: apply creates and activates "PCOptimizer Gaming" or "PCOptimizer Ultimate", undo activates the previous plan and deletes the created one. Possible fix: count only conflicts the app changed itself (`store.Get(o) is not null`), or let the conflicting tweak be replaced in one step.

### B2. Undo of `memory.compressionOff` does nothing

`Disable-MMAgent -MemoryCompression` takes effect only after a restart. Right after apply, the engine reads the value back (still `True`) and stores it as the applied value, which equals the original. After the restart the real value is `False`. On undo, `IsStillApplied` compares `False` with the stored `True`, treats the entry as reset by Windows, skips `Enable-MMAgent` (the log shows no call) and archives the backup. Memory compression stays off after another restart, and no backup is left.

Before the restart the state shows as RevertedByWindows instead of PendingRestart. Together with audit H5, the inspector then offers "Apply again", which runs Undo.

Possible fix: for actions that take effect after a restart, store the desired value as the applied value (or let the action report its configured value instead of the running value), and add a sandbox test with a fake that applies only after a simulated restart.

### B3. `power.hibernateOff` cannot be undone where hibernation is unsupported

In this VM the firmware does not support hibernation. Apply still succeeds (it writes `HibernateEnabled=0`; the value was not set before). Undo runs `powercfg /hibernate on`, which fails with "The system firmware does not support hibernation". The backup stays, and the tweak keeps showing as applied. Possible fix: make the tweak not applicable when `powercfg /a` reports hibernation as unavailable, or restore only the registry value in that case.

### B4. `background.widgetsOff` fails on build 26300.9550

On 26300.9550 Windows denies writing `HKLM\SOFTWARE\Policies\Microsoft\Dsh\AllowNewsAndInterests`, also for an elevated administrator and also with `reg.exe`, while other values in the same key can be written. On 26300.9457 (the `clean` checkpoint) the same write works, so the protection came with that cumulative update. The User Choice Protection Driver (`UCPD`) is running and is the likely cause (not confirmed). The engine rolls back cleanly and nothing is left, but the user only sees "Attempted to perform an unauthorized operation". Possible fix: detect the denied write and show the tweak as unsupported on this build, with a short explanation.

### B5. DNS presets and `network.nagleOff` skip the Hyper-V network adapter

`NicAdapters.Enumerate` (`src/Optimizer.Core/Actions/ExtendedActions.cs:181`) accepts only device ids that start with `PCI\` or `USB\`. The Hyper-V adapter is `VMBUS\...`, so `network.dns.cloudflare`, `network.dns.google`, `network.dns.quad9` and `network.nagleOff` are Unsupported in any Hyper-V VM, although the plan lists them in section 4. This is by design for hardware properties, but DNS servers and TCP settings are not hardware properties. Related to audit M35. Either move these four to section 5 or let DNS and TCP tweaks accept synthetic adapters.

## Notes

- **N1. Empty policy keys after undo (19 tweaks).** Undo removes the value but not the key that apply created, for example `HKLM\SOFTWARE\Policies\Microsoft\Edge`. Behavior is the original one. Tweaks: `power.throttlingOff`, `network.deliveryOptimizationP2POff`, `background.backgroundAppsOff`, `background.aiOff`, `explorer.endTask`, `privacy.advertisingIdOff`, `privacy.inkingTypingOff`, `privacy.webSearchOff`, `privacy.searchHighlightsOff`, `privacy.findMyDeviceOff`, `privacy.errorReportingOff`, `privacy.ceipOff`, `privacy.appCompatTelemetryOff`, `privacy.settingsSyncOff`, `privacy.messageSyncOff`, `privacy.mapsTrafficOff`, `background.edgeBoostOff`, `updates.driversExcluded`, `privacy.deviceMetadataOff`.
- **N2. `DelayedAutostart=0` after undo.** `privacy.telemetryOff` (DiagTrack) and `services.gamingPreset` (TrkWks) write `DelayedAutostart=0` on undo where the value did not exist. Same behavior.
- **N3. Power mode after undo.** `quiet.powerModeEfficiency` leaves the overlay set to the empty GUID (Balanced) where no overlay value existed. Same behavior. Only the AC overlay changed, as audit M7 describes.
- **N4. Missing scheduled tasks.** `\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser` and `ProgramDataUpdater` (part of `privacy.telemetryOff`) do not exist on build 26300. The engine skips them and still reports the tweak as applied.
- **N5. No pending state for undo.** After undoing `security.vbsOff` and `memory.compressionOff`, the state changes at once, although the system changes only after the next restart.
- **N6. Audit M4.** With UAC on (classic), backup files are owned by `BUILTIN\Administrators`, so they are trusted. UAC off, the built-in Administrator account and Administrator protection were not tested.

## Results per tweak

| Tweak | Section | Result |
|---|---|---|
| `security.vbsOff` | 2 | OK. Precondition: memory integrity turned on (VBS status 2). After apply and restart VBS is off; after undo and restart VBS and HVCI run again. Windows started every time. |
| `leftover.usePlatformClock` | 2 | OK. Precondition: `bcdedit /set {current} useplatformclock true`. Removed by apply, still gone after restart, `Yes` again after undo and restart. |
| `power.throttlingOff` | 3 | OK, N1 |
| `gpu.hags` | 3 | Not applicable: no GPU with HAGS support |
| `gpu.mpoOff` | 3 | OK |
| `memory.compressionOff` | 3 | B2 |
| `memory.sysmainOff` | 3 | Not applicable: the virtual disk is not reported as SSD |
| `memory.pagefileSystemManaged` | 3 | Already set |
| `storage.lastAccessOff` | 3 | OK |
| `network.throttlingIndex` | 3 | OK |
| `network.preferIpv4` | 3 | OK |
| `privacy.telemetryOff` | 3 | OK, N2, N4 |
| `visual.bestPerformance` | 3 | OK (registry values; the visual effect after sign-out was not checked) |
| `explorer.classicContextMenu` | 3 | OK (registry values; the menu itself was not checked) |
| `privacy.appCompatTelemetryOff` | 3 | OK, N1 |
| `privacy.phoneLinkOff` | 3 | OK |
| `privacy.crossDeviceOff` | 3 | OK |
| `system.registryBackup` | 3 | OK |
| `power.balancedPlan` | 4 | Already set |
| `power.gamingPlan` | 4 | B1; OK with Power saver active |
| `power.ultimatePlan` | 4 | B1; OK with Power saver active |
| `power.turboRestore` | 4 | Already set |
| `power.usbSelectiveSuspendOff` | 4 | OK |
| `power.pcieAspmOff` | 4 | OK |
| `power.fastStartupOff` | 4 | OK |
| `power.hibernateOff` | 4 | B3 |
| `gpu.windowedOptimizations` | 4 | Not applicable: no discrete GPU |
| `gpu.gameMode` | 4 | Already set |
| `gpu.gameDvrOff` | 4 | OK (detected as Partial before and after) |
| `input.mouseAccelOff` | 4 | OK |
| `storage.trimOn` | 4 | Already set |
| `storage.storageSenseOn` | 4 | Already set |
| `network.nagleOff` | 4 | B5 |
| `network.deliveryOptimizationP2POff` | 4 | OK, N1 |
| `privacy.activityHistoryOff` | 4 | OK |
| `privacy.consumerFeaturesOff` | 4 | Not applicable: Pro edition |
| `privacy.locationOff` | 4 | OK |
| `background.backgroundAppsOff` | 4 | OK, N1 |
| `background.aiOff` | 4 | OK, N1 |
| `background.widgetsOff` | 4 | B4 |
| `visual.transparencyOff` | 4 | OK |
| `explorer.fileExtensions` | 4 | OK |
| `explorer.endTask` | 4 | OK, N1 |
| `network.dns.cloudflare` | 4 | B5 |
| `network.dns.google` | 4 | B5 |
| `network.dns.quad9` | 4 | B5 |
| `privacy.advertisingIdOff` | 4 | OK, N1 |
| `privacy.tailoredExperiencesOff` | 4 | OK |
| `privacy.feedbackNotificationsOff` | 4 | OK |
| `privacy.diagnosticLogsLimited` | 4 | OK |
| `privacy.inkingTypingOff` | 4 | OK, N1 |
| `privacy.onlineSpeechOff` | 4 | OK |
| `privacy.webSearchOff` | 4 | OK, N1 |
| `privacy.searchHighlightsOff` | 4 | OK, N1 |
| `privacy.cloudSearchOff` | 4 | OK |
| `privacy.cloudClipboardOff` | 4 | OK |
| `privacy.suggestionsOff` | 4 | OK |
| `privacy.onlineTipsOff` | 4 | OK |
| `privacy.appLaunchTrackingOff` | 4 | OK |
| `privacy.languageListOff` | 4 | OK |
| `privacy.findMyDeviceOff` | 4 | OK, N1 |
| `privacy.errorReportingOff` | 4 | OK, N1 |
| `privacy.ceipOff` | 4 | OK, N1 |
| `privacy.appPersonalDataOff` | 4 | OK |
| `privacy.settingsSyncOff` | 4 | OK, N1 |
| `privacy.messageSyncOff` | 4 | OK, N1 |
| `privacy.mapsTrafficOff` | 4 | OK, N1 |
| `privacy.clipboardHistoryOff` | 4 | OK |
| `services.gamingPreset` | 4 | OK, N2 |
| `battery.boostOffDc` | 4 | Not applicable: no battery |
| `battery.wifiPowerSavingDc` | 4 | Not applicable: no battery |
| `battery.pcieAspmMaxDc` | 4 | Not applicable: no battery |
| `quiet.boostOff` | 4 | B1; cannot be applied on a default PC |
| `quiet.powerModeEfficiency` | 4 | OK, N3 |
| `office.clipboardHistoryOn` | 4 | OK |
| `office.launchToThisPc` | 4 | OK |
| `background.edgeBoostOff` | 4 | OK, N1 |
| `updates.driversExcluded` | 4 | OK, N1 |
| `privacy.deviceMetadataOff` | 4 | OK, N1 |

## Retest after the fixes

Same VM, build 26300.9457, after the fixes for B1 to B5 (engine and adapter changes with sandbox tests).

| Bug | Tweaks | Result |
|---|---|---|
| B1 | `power.gamingPlan`, `power.ultimatePlan`, `quiet.boostOff` | OK with Balanced and default boost active. Boost mode (registry `ACSettingIndex` and `DCSettingIndex`) went from 2 to 0 and back to 2. |
| B2 | `memory.compressionOff` | OK. PendingRestart after apply, off after the restart, undo ran `Enable-MMAgent`, on again after the next restart, backup removed. Until that restart the state still reads Applied (N5). |
| B3 | `power.hibernateOff` | Unsupported without S4, apply does nothing. A backup from version 0.4.0 can be undone (sandbox test). |
| B4 | `background.widgetsOff` | OK, N1 (the value can be written on 9457). On builds that deny the write, the error now names the value and says Windows protects it. |
| B5 | `network.dns.cloudflare`, `network.dns.google`, `network.dns.quad9`, `network.nagleOff` | OK. DNS went from the DHCP server to the preset and back; `TcpAckFrequency` was written and removed again. |

## Not covered

- **Section 1 (general flow):** needs the UI: confirmation dialogs, restore point question, Changes page, Licenses window, reset detection banner, language and theme, update check. Also check audit H5 ("Apply again" runs Undo) and H6 (undo failure message) there.
- **Section 5:** needs a real NVIDIA GPU and a physical network adapter.
- **Section 6:** runtime-built changes (startup entries, services, tasks, features, debloat, cleanup, storage analyzer, apps, tools, health) go through the UI and were not run.

## Suggested plan changes

- Done in this run: VM memory raised to 16 GB in the plan.
- Section 2 needs preconditions, or both tweaks have nothing to change on a clean install: turn on memory integrity before `security.vbsOff`, and set `useplatformclock` before `leftover.usePlatformClock`. The section 2 text is generated by `DocsConsistencyTests`, so add the preconditions there.
- B5 is fixed: the DNS presets and `network.nagleOff` can stay in section 4.
- Note that `memory.sysmainOff` cannot be tested in Hyper-V.
- Retake the `clean` checkpoint after all updates are installed: this run's checkpoint was on 26300.9457 while the VM had already updated to 26300.9550.
