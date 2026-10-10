# VM test results, 2026-10-10

Run of [vm-test-plan.md](vm-test-plan.md) sections 2, 3 and 4 at commit 20650b3 (version 0.4.0). A second run covered sections 1 and 6 through the UI at commit 2d781ff (version 0.4.4), see [Sections 1 and 6](#sections-1-and-6-ui). Section 5 needs real hardware (see [Not covered](#not-covered)).

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
- **N5. No pending state for undo.** After undoing `security.vbsOff` and `memory.compressionOff`, the state changes at once, although the system changes only after the next restart. Fixed for memory compression with the state "Off after a restart" (see the retest). `security.vbsOff` reads the configured registry values, so its state is right; only the running VBS changes later.
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
| B2 | `memory.compressionOff` | OK. PendingRestart after apply, off after the restart, undo ran `Enable-MMAgent`, on again after the next restart, backup removed. Retest on 26300.9550 with the new state: "Off after a restart" between the undo and the restart, then NotApplied, and the pending undo record was removed. |
| B3 | `power.hibernateOff` | Unsupported without S4, apply does nothing. A backup from version 0.4.0 can be undone (sandbox test). |
| B4 | `background.widgetsOff` | OK, N1. After the VM installed a newer UCPD driver (same build 26300.9550), the write works again, so whether Windows blocks the value depends on the UCPD driver version, not on the build. On a PC that denies the write, the error now names the value and says Windows protects it. |
| B5 | `network.dns.cloudflare`, `network.dns.google`, `network.dns.quad9`, `network.nagleOff` | OK. DNS went from the DHCP server to the preset and back; `TcpAckFrequency` was written and removed again. |

## Sections 1 and 6 (UI)

Same VM on build 26300.9550, restored from the `clean` checkpoint (System Protection off, local account, OneDrive installed but not signed in). The release exe ran elevated in the signed-in console session, started through a scheduled task with the highest run level, so there was no UAC prompt; the manifest of the published exe requests `requireAdministrator`. Every step was driven through UI Automation (and the mouse where a control has no automation pattern), with screenshots, and checked independently over PowerShell Direct. The exe was built from the working tree that was then committed as 2d781ff; the fixes for B6 to B9 were checked again with a new build.

### Section 1: general flow

| Step | Result |
|---|---|
| First start | OK. Scan finished in about 1.6 s, Gaming profile suggested with an info bar, no error banner. Readiness 93, 1 manual check, 14 passed, 2 recommended. |
| Licenses window | OK. All 17 components show their full license text. |
| Apply recommended | OK. One confirmation with the exact registry values (mouse acceleration, transparency), nothing Expert, boot-critical or anti-cheat sensitive. |
| Restore point | OK. With System Protection off the app asked first; "Turn on and continue" turned it on, set `SystemRestorePointCreationFrequency` to 0 (listed on the Changes page with its own undo) and created one restore point. The next app session created one more before its first change. |
| After apply | OK. All four values written, both tweaks On, Changes page lists them with the change log. |
| Restart | OK. Both still On. No item of this PC's recommendation waits for a restart. |
| Undo all | OK. One confirmation ("Changes to undo: 3"), all four values and the restore point frequency back to the originals, Changes page empty, backups removed. |
| Restart again | OK. Windows started normally; the scan matched the first one (93, same 2 recommendations). |
| Reset detection | OK. "Show file extensions" applied, `HideFileExt` set back to 1 by hand, Scan again: the banner named the change and offered "Apply again". |
| Apply again (audit H5) | OK, fixed. The button applied the change again (`HideFileExt` 0), the banner disappeared and the backup from the first change stayed, so undo restored 1. |
| Undo failure message (audit H6) | Not triggered in the VM; the code now uses `Result_UndoIncomplete` for a failed undo and `Result_Error` for other errors. |
| Windows update between apply and undo | Not tested: no cumulative update newer than 26300.9550 was offered. |
| Language and theme | OK. Deutsch and Dark kept after restarting the app. |
| Update check | OK. Turned on, restarted the app: the log shows the release check with latest 0.3.0, no banner because 0.4.4 is newer, no error. A banner for a newer release could not be tested. |

### Section 6: runtime changes and other features

| Feature | Result |
|---|---|
| Startup entries | OK. A test Run key entry and a Startup folder shortcut switched off (StartupApproved `03`, like Task Manager); after sign out and in neither started while Steam did. Switched on again: both started. Found B6 and B7. |
| Services | OK. Distributed Link Tracking Client from Automatic to Manual, after a restart Manual and stopped, Windows fine, undo set Automatic again. |
| Scheduled tasks | OK. `MicrosoftEdgeUpdateTaskMachineUA` off (Disabled) and on again (Ready). |
| Windows features | OK. Telnet Client on, restart, still on, off again. |
| Debloat | OK. Solitaire Collection and Microsoft News removed for the user and from provisioning, so new accounts do not get them; both listed under removed apps; the Store button opened the product page and installed Microsoft News again. |
| OneDrive | OK. With Known Folder Move (Documents redirected into the OneDrive folder, simulated in the registry because the account is not signed in) the uninstall button was disabled with the reason. Without it, `OneDriveSetup.exe /uninstall` ran as the user and OneDrive was gone. |
| Cleanup | OK. All 11 categories selected. "Your temporary files" showed 4 MB, 10 files, matching an independent count (4306 KB, 10 files with both times older than 24 hours); the Recycle Bin showed the 300 KB test file. Afterwards the test files were gone, a file held open was skipped, the signed-in user's Recycle Bin was empty and a second user's Recycle Bin was untouched. |
| Storage analyzer | OK after the B9 fix. The duplicate in `C:\Test\dup2` went to the Recycle Bin, the first copy stayed; files in Windows, Program Files and ProgramData cannot be selected. |
| Apps | OK. Discord (per user) installed with winget running as the user; 7-Zip (machine-wide) installed to Program Files. Discord's own first start then asked for UAC for its helper; that prompt comes from Discord. |
| DNS presets | OK after the B8 fix. Cloudflare set 1.1.1.1 and 1.0.0.1 on the Hyper-V adapter, undo restored DHCP (172.19.16.1) and an empty `NameServer`. |
| DNS benchmark | OK. Results for the current server, Quad9, Cloudflare and Google; DNS settings unchanged. |
| Tools | OK: DNS flush (`ipconfig /flushdns`), Explorer restart (the new Explorer runs without elevation), Winsock reset (network and DNS work after the restart). Windows Update repair partly: SoftwareDistribution renamed and services started again, but renaming catroot2 was denied (U9); the page lists the failed step and says the repair was partial. |
| Health | OK. SFC and DISM ScanHealth with live output until the result and exit code 0. Throttle check: 53 samples, no throttling. PresentMon 2.6.0 started and reported "No frames captured" for explorer.exe (no game in the VM). PawnIO installed from the Health page through winget without a prompt; `winget uninstall` removed the files but left the driver package (`oem2.inf`) and its service, also after a restart, so the page still says installed (U10). |
| Per-game NVIDIA profiles, MSI mode | Not tested: need real hardware. |

### Bugs (fixed)

**B6. Startup folder shortcuts with arguments show "File not found".** `StartupScanner.StartupFolders` passed the whole `"target" arguments` string as the image path, so the signature check never found the file and the entry showed "No publisher, File not found" (a shortcut to notepad.exe in the test). The image is now taken from the target alone.

**B7. rundll32 entries with a switch show "File not found".** `CommandLine.ImagePath` took the first argument as the DLL, which is `/d` in Windows' `Autochk\Proxy` task. Switches are now skipped.

**B8. The three DNS presets look the same.** All three share the explanation page and had no subject, so each row read "Public DNS servers" with the same text. They now read "Public DNS servers: Cloudflare (1.1.1.1)", "Google (8.8.8.8)" and "Quad9 (9.9.9.9)", and `CatalogIntegrityTests` fails when tweaks share a page without distinct subjects.

**B9. The storage analyzer offers `pagefile.sys`.** Only folders were protected, so `C:\pagefile.sys` (and `swapfile.sys`) could be selected; Windows would refuse to move them because they are in use. Page file, swap file, hibernation file and boot dump log in the root of a drive are now protected.

### UI findings (fixed)

- **U1.** Changes made outside the catalog (restore point frequency, startup entries, services) show "Values: 1" with no state after it on the Changes page. Fixed: their state is read in the background with the backups, so they show On or Off.
- **U2.** Counts use a fixed plural: "1 changes" in the Apply again dialog, "1 files:" in the Recycle Bin dialog. Fixed: counts that can be 1 use the "Label: {0}" form in English and German ("Changes: 1", "Files: 1").
- **U3.** The undo confirmation also says "The original values are saved before anything changes.", which belongs to apply. Fixed: the undo confirmation says "Values that cannot be restored stay on the Changes page."
- **U4.** The dialog for a scheduled task that runs on a schedule is titled "Scheduled task at sign-in or boot". Fixed: the page is titled "Scheduled task", which fits every task on the Scheduled tasks tab.
- **U5.** The SFC and DISM output box stays white in dark mode, and every progress step (`Verification 1% complete.`, the DISM progress bar) becomes its own line: 177 lines for one SFC run. Fixed: the box uses the WPF-UI text box style, and a progress line replaces the one before it when only the number changed (5 lines for an SFC run in the retest).
- **U6.** The throttle check shows "Graphics card at temperature limit ? %, at power limit ? %" when there is no graphics card data; a sentence that says the data is not available would read better. Fixed: without limit reasons from the graphics card the result says so ("only NVIDIA graphics cards report them"), and the live line says "not reported".
- **U7.** The DNS confirmation names the adapter only by its GUID; the removed apps list shows package names (`Microsoft.BingNews`) instead of the app names. Fixed: the DNS confirmation names the adapter ("Microsoft Hyper-V Network Adapter") before the GUID, and removed apps show their name from the catalog.
- **U8.** Accessibility: list items in the Licenses window, the removed apps list and the Startup, Services and Tools rows have no automation name, so screen readers read the type (`Optimizer.App.ViewModels.StartupRow`, `LicenseComponent { Name = ... }`). The language radio buttons use `Command`, which a screen reader's select action does not trigger, and a toggle switch confirmation blocks the UI Automation call until the dialog closes. Fixed: every list and combo box item has a name (its title), the language buttons react to a check instead of a click, and a row switch returns before its confirmation opens (a toggle call now returns in under 20 ms).
- **U9.** Windows Update repair could not rename catroot2 ("Access denied") although Cryptographic Services had been stopped three seconds earlier; it is trigger-started, so something probably started it again. Renaming catroot2 right after stopping the service, or retrying, would help. Fixed: when the rename is denied, Cryptographic Services is stopped again and the rename retried, up to three attempts. In the retest the rename worked on the first attempt; the retry is covered by a test.
- **U10.** After `winget uninstall namazso.PawnIO` the driver stays installed, so the hint "can be uninstalled in Settings > Apps" promises more than it does; removing it fully needs `pnputil /delete-driver oem2.inf /uninstall`. Fixed: the PawnIO install text says that uninstalling can leave the driver and how to remove it with pnputil.

## Preview review

After sections 1 to 4 and 6, each of the 26 Preview tweaks was decided on. Same VM, build 26300.9550, restored from `clean`, version 0.4.7, apply and undo through the console runner. Where the effect could only be seen in Windows, it was read where a user sees it: in Settings, in File Explorer, in Start search, or through `SystemParametersInfo` after signing in again.

| Tweak | Check | Result |
|---|---|---|
| `memory.pagefileSystemManaged` | Precondition: a page file on C: with automatic management off (`C:\pagefile.sys 0 0`). Apply, restart, undo, restart | OK. `AutomaticManagedPagefile` (the "Automatically manage paging file size" checkbox) true after apply and the restart, false again after undo with `C:\pagefile.sys 0 0` restored. |
| `storage.storageSenseOn` | Precondition: Storage Sense off. The switch on Settings > System > Storage > Storage Sense | OK. Off, On after apply, Off after undo. The policy key exists only after the Storage Sense page was opened once. |
| `visual.bestPerformance` | `SystemParametersInfo` after signing in again | OK. Window, menu, combo box, tooltip, selection and minimize animations, shadows, smooth scrolling and "show window contents while dragging" off after apply; font smoothing stays on; undo restores every value and the same `UserPreferencesMask`. |
| `explorer.classicContextMenu` | Right-click a file in File Explorer after signing in again | OK. Classic menu with "Send to" and "Create shortcut" and no "Show more options"; after undo the Windows 11 menu with "Show more options" again; the CLSID key is removed. |
| `office.launchToThisPc` | Title of a new File Explorer window | OK. "Home", "This PC" after apply, "Home" after undo. |
| `privacy.suggestionsOff` | Switches in Settings after signing in again | OK. "Recommendations and offers in Settings", "Show tips and app recommendations" (Start) and "Get tips and suggestions when using Windows" Off after apply and On after undo. |
| `privacy.webSearchOff` | Start search for "weather" after signing in again | OK. No "Microsoft Bing web suggestions" and no Microsoft Bing tab after apply; both back after undo. |
| `privacy.inkingTypingOff` | Switches in Settings after signing in again | Partly. "Improve inking and typing" Off and locked after apply, On after undo; but "Custom inking and typing dictionary" stays On although both `RestrictImplicit*Collection` policies are set, and "Typing insights" does not show in the VM. |

Decision:

- **No longer Preview (13):** the seven above marked OK, plus `power.ultimatePlan` and `memory.compressionOff` (B1 and B2 fixed and retested), the three DNS presets (B5 fixed and retested, and the UI test in section 6) and `services.gamingPreset` (OK, N2 does not change behavior).
- **Still Preview (13):** `security.vbsOff`, `leftover.usePlatformClock` and `network.interruptModerationOff` (Expert or boot-critical, always Preview); the four NVIDIA settings and the two network adapter properties (need real hardware); `gpu.hags` and `gpu.mpoOff` (no GPU in the VM that supports them); `gpu.gameDvrOff` (detected as Partial before and after apply); `privacy.inkingTypingOff` (the dictionary switch above).

## Not covered

- **Section 1:** the Windows update between apply and undo (no newer cumulative update), the update banner for a newer release (no newer release published) and a real undo failure for the H6 message.
- **Section 5:** needs a real NVIDIA GPU and a physical network adapter.
- **Section 6:** per-game NVIDIA profiles and MSI mode need real hardware.

## Suggested plan changes

- Done in this run: VM memory raised to 16 GB in the plan.
- Section 2 needs preconditions, or both tweaks have nothing to change on a clean install: turn on memory integrity before `security.vbsOff`, and set `useplatformclock` before `leftover.usePlatformClock`. The section 2 text is generated by `DocsConsistencyTests`, so add the preconditions there.
- B5 is fixed: the DNS presets and `network.nagleOff` can stay in section 4.
- Note that `memory.sysmainOff` cannot be tested in Hyper-V.
- Retake the `clean` checkpoint after all updates are installed: this run's checkpoint was on 26300.9457 while the VM had already updated to 26300.9550.
