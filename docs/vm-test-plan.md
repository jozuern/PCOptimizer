# VM test plan

Real apply and undo round trips for every change the app can make. Unit tests only cover the registry sandbox and fakes; this plan covers real Windows. Run it before each public release, in a Hyper-V VM, never on your own PC. The last run and its findings: [vm-test-results-2026-10-10.md](vm-test-results-2026-10-10.md).

The tweak tables in sections 2 to 5 are generated from the catalog: after a catalog change, run `dotnet test` once with the environment variable `PCO_UPDATE_DOCS=1`.

## Setup

1. Hyper-V VM, generation 2, Windows 11 24H2 or newer x64 (Pro or Home), Secure Boot and virtual TPM on, 2 or more virtual processors, 16 GB memory (`memory.compressionOff` applies from 16 GB), 80 GB disk. Use an unmodified Microsoft ISO: debloated images change the starting state the tests compare against. See [Creating the VM](#creating-the-vm).
2. Install all Windows updates, sign in with a local administrator account, install Steam or another launcher so game library checks have something to find.
3. Copy the Release exe (`dotnet publish src/Optimizer.App -p:PublishProfile=SingleFile`, then `artifacts/publish/PCOptimizer.exe`) into the VM.
4. Take a checkpoint named **clean**. Go back to it whenever a test leaves the VM in a state you do not trust.
5. Keep `%ProgramData%\PCOptimizer\logs` open: every apply, verify and undo is logged there. Attach the log to any bug report.

For each tweak: apply it alone, check that the state shows **On** and that the change is really there (Settings, regedit, `powercfg /q`, `sc qc`, `schtasks /query`, `bcdedit`), restart or sign out where the table says so, then **Undo** on the Changes page and check that the original value is back. Write OK or the problem into the Result column.

### Creating the VM

Hyper-V needs Windows 11 Pro on the host: turn on **Hyper-V** in "Turn Windows features on or off" and restart. Then run this in an elevated PowerShell (change `$iso`):

```powershell
$name = 'PCO-Test'; $root = 'C:\Hyper-V'; $iso = "$env:USERPROFILE\Downloads\Win11_x64.iso"
New-VM -Name $name -Generation 2 -MemoryStartupBytes 16GB -Path $root -NewVHDPath "$root\$name\$name.vhdx" -NewVHDSizeBytes 80GB -SwitchName 'Default Switch'
Set-VMMemory -VMName $name -DynamicMemoryEnabled $false
Set-VMProcessor -VMName $name -Count 4 -ExposeVirtualizationExtensions $true
Set-VM -Name $name -CheckpointType Production -AutomaticCheckpointsEnabled $false
Set-VMKeyProtector -VMName $name -NewLocalKeyProtector; Enable-VMTPM -VMName $name
$dvd = Add-VMDvdDrive -VMName $name -Path $iso -Passthru; Set-VMFirmware -VMName $name -FirstBootDevice $dvd
Get-VMIntegrationService -VMName $name | Enable-VMIntegrationService
Add-LocalGroupMember -SID 'S-1-5-32-578' -Member ([Security.Principal.WindowsIdentity]::GetCurrent().Name)
```

`ExposeVirtualizationExtensions` lets memory integrity run in the VM, so `security.vbsOff` has something to turn off. Membership in Hyper-V Administrators (SID `S-1-5-32-578`, active after the next sign-in) lets the commands below run without UAC.

1. Open **Hyper-V Manager**, double-click the VM, click **Start** and press a key at "Press any key to boot from CD or DVD". If you miss it, the VM shows a boot error: use **Action > Reset** and try again.
2. Install Windows. For a local account, press Shift+F10 on the network or account screen and run `start ms-cxh:localonly`. If newer builds remove that route, sign in with a Microsoft account, add a local account in Settings > Accounts > Other users ("Add a user without a Microsoft account"), make it an administrator, and use only that account. Give it a password: the enhanced session (shared clipboard, window resizing) needs one.
3. An unactivated Windows works for these tests, but Settings > Personalization is locked; check those values in regedit instead.
4. Copy the exe from the host: `Copy-VMFile -Name PCO-Test -SourcePath artifacts\publish\PCOptimizer.exe -DestinationPath C:\Test\PCOptimizer.exe -CreateFullPath -FileSource Host` (the VM must be running), or copy and paste the file in an enhanced session.
5. Checkpoints: `Checkpoint-VM -Name PCO-Test -SnapshotName clean` to take one, `Restore-VMCheckpoint -VMName PCO-Test -Name clean -Confirm:$false` to go back (or right-click the VM or the checkpoint in Hyper-V Manager). Production checkpoints do not keep the running state: after a restore the VM is off and starts fresh.

## 1. General flow

| Step | Expected | Result |
|---|---|---|
| First start | UAC prompt, scan finishes, a profile is suggested, no error banner | |
| Settings > About > Licenses | Window opens, every component shows its text | |
| Apply recommended | One confirmation listing each item with its reason; no Expert, boot-critical or anti-cheat sensitive item in it | |
| Restore point | Created once before the first change of the session (System Protection on), or the app asks to turn it on | |
| After apply | Every item verified as On, all listed on the Changes page | |
| Restart | States still On; items marked "after restart" now active | |
| Undo all | Every original value back (spot check 5 in regedit), Changes page empty | |
| Restart again | Windows starts normally, scan shows the same state as before the apply | |
| Reset detection | Apply "Show file extensions", turn it off by hand in Explorer options, scan again: banner says the change was reset, "Apply again" works | |
| Windows update between apply and undo | Checkpoint, apply recommended, install a cumulative update, scan: reset changes are listed with the update as likely cause; Undo still restores the originals | |
| Language and theme | Switch to Deutsch and Dark in Settings, restart the app: both kept | |
| Update check | Turn on, restart the app: no error; with a newer published release a banner links to the release page | |

<!-- Generated from the tweak catalog by DocsConsistencyTests. Do not edit by hand. -->

The tables below cover all 211 catalog tweaks. Preview tweaks are the risky ones; test each of them on its own.

## 2. Expert and boot-critical tweaks

Turn on Expert mode. Take a checkpoint before **each** of these, apply it alone, restart, confirm Windows still starts, then undo and restart again.

Preconditions, or there is nothing to change on a clean install: turn on memory integrity (Windows Security > Device security > Core isolation) and restart before `security.vbsOff`; run `bcdedit /set {current} useplatformclock true` before `leftover.usePlatformClock`.

| Tweak | Title | Risk | Then | Actions | Result |
|---|---|---|---|---|---|
| `security.vbsOff` | Virtualization-based security and memory integrity off | expert, boot-critical, anti-cheat sensitive, preview | restart | registry | |
| `leftover.usePlatformClock` | Remove forced platform clock (useplatformclock) | expert, boot-critical, preview | restart | bcd | |
| `expert.legacyBootMenu` | Classic F8 boot menu | expert, boot-critical, preview | restart | bcd | |

## 3. Tweaks that need a restart or sign-out

| Tweak | Title | Risk | Then | Actions | Result |
|---|---|---|---|---|---|
| `power.throttlingOff` | Power throttling off | safe | restart | registry | |
| `gpu.hags` | Hardware-accelerated GPU scheduling (HAGS) | safe, preview | restart | registry | |
| `gpu.mpoOff` | Multiplane overlay (MPO) off | moderate, preview | restart | registry | |
| `memory.compressionOff` | Memory compression off | moderate | restart | memoryCompression | |
| `memory.sysmainOff` | SysMain (Superfetch) off | moderate | restart | service | |
| `memory.pagefileSystemManaged` | Page file managed by Windows | moderate | restart | registry | |
| `storage.lastAccessOff` | NTFS last-access timestamps off | safe | restart | registry | |
| `network.throttlingIndex` | Network throttling off (NetworkThrottlingIndex) | safe | restart | registry | |
| `network.preferIpv4` | Prefer IPv4 over IPv6 | safe | restart | registryBits | |
| `privacy.telemetryOff` | Telemetry to minimum | moderate | restart | registry, service, scheduledTask | |
| `visual.bestPerformance` | Visual effects: best performance | safe | sign out | registry, registryBinaryBits | |
| `explorer.classicContextMenu` | Classic right-click menu | safe | sign out | registry | |
| `privacy.appCompatTelemetryOff` | Application compatibility telemetry off | moderate | restart | registry | |
| `privacy.phoneLinkOff` | Phone-PC linking off | moderate | restart | registry | |
| `privacy.crossDeviceOff` | Continue experiences on other devices off | moderate | restart | registry | |
| `system.registryBackup` | Registry backup to the RegBack folder | safe | restart | registry | |
| `ai.recallOff` | Recall removed | moderate | restart | registry | |
| `focus.notificationCenterOff` | Notification center removed from the taskbar | moderate | restart | registry | |
| `personalize.taskbarSearchHidden` | Taskbar: search hidden | safe | sign out | registry | |
| `personalize.taskbarSearchIcon` | Taskbar: search as an icon only | safe | sign out | registry | |
| `personalize.taskViewHidden` | Taskbar: Task View button hidden | safe | sign out | registry | |
| `personalize.startRecentlyAddedHidden` | Start: no recently added apps | safe | sign out | registry | |
| `personalize.startMostUsedHidden` | Start: no most used apps | safe | sign out | registry | |
| `system.longPaths` | Long file paths | safe | restart | registry | |
| `security.lsaProtection` | LSA protection on | moderate | restart | registry | |
| `security.defenderSandbox` | Microsoft Defender in a sandbox | safe | restart | registry | |
| `personalize.desktopThisPc` | This PC icon on the desktop | safe, preview | sign out | registry | |
| `personalize.titleBarShakeOff` | No minimizing by shaking a window | safe | sign out | registry | |
| `input.printScreenSnippingOff` | Print Screen copies the screen again | safe, preview | sign out | registry | |
| `explorer.fullPathTitle` | File Explorer: full path in the title | safe, preview | sign out | registry | |

## 4. Other tweaks

`memory.sysmainOff` cannot be tested in Hyper-V: the virtual disk is not reported as an SSD, so the tweak does not apply there.

| Tweak | Title | Risk | Then | Actions | Result |
|---|---|---|---|---|---|
| `power.balancedPlan` | Switch to the Balanced power plan | safe |  | powerScheme | |
| `power.gamingPlan` | Gaming power plan (based on High performance) | safe |  | powerScheme | |
| `power.ultimatePlan` | Ultimate Performance power plan | moderate |  | powerScheme | |
| `power.turboRestore` | Restore processor turbo | safe |  | powerSetting | |
| `power.usbSelectiveSuspendOff` | USB selective suspend off | safe |  | powerSetting | |
| `power.pcieAspmOff` | PCIe link power management off | safe |  | powerSetting | |
| `power.fastStartupOff` | Fast Startup off | safe |  | registry | |
| `power.hibernateOff` | Hibernation off | safe |  | hibernation | |
| `gpu.windowedOptimizations` | Optimizations for windowed games | safe |  | registryToken | |
| `gpu.gameMode` | Game Mode on | safe |  | registry | |
| `gpu.gameDvrOff` | Game Bar captures off | safe, preview |  | registry | |
| `input.mouseAccelOff` | Mouse acceleration off | safe |  | registry | |
| `storage.trimOn` | TRIM on | safe |  | registry | |
| `storage.storageSenseOn` | Storage Sense on | safe |  | registry | |
| `network.nagleOff` | Delayed TCP acknowledgements off (TcpAckFrequency) | moderate |  | registry | |
| `network.deliveryOptimizationP2POff` | Delivery Optimization peer-to-peer off | safe |  | registry | |
| `privacy.activityHistoryOff` | Activity history off | safe |  | registry | |
| `privacy.consumerFeaturesOff` | Consumer features off | safe |  | registry | |
| `privacy.locationOff` | Location access off | safe |  | registry | |
| `background.backgroundAppsOff` | Background apps off | moderate |  | registry | |
| `background.aiOff` | Recall snapshots off | moderate |  | registry | |
| `background.widgetsOff` | Widgets off | safe |  | registry | |
| `visual.transparencyOff` | Transparency effects off | safe |  | registry | |
| `explorer.fileExtensions` | Show file extensions | safe |  | registry | |
| `explorer.endTask` | "End task" in the taskbar menu | safe |  | registry | |
| `network.dns.cloudflare` | Public DNS servers | safe |  | dns | |
| `network.dns.google` | Public DNS servers | safe |  | dns | |
| `network.dns.quad9` | Public DNS servers | safe |  | dns | |
| `network.dns.opendns` | Public DNS servers | safe, preview |  | dns | |
| `network.dns.adguard` | Public DNS servers | safe, preview |  | dns | |
| `network.dns.automatic` | DNS servers: automatic (from the router) | safe, preview |  | dns | |
| `privacy.advertisingIdOff` | Advertising ID off | safe |  | registry | |
| `privacy.tailoredExperiencesOff` | Tailored experiences off | safe |  | registry | |
| `privacy.feedbackNotificationsOff` | Feedback requests off | safe |  | registry | |
| `privacy.diagnosticLogsLimited` | Limit diagnostic logs and memory dumps | safe |  | registry | |
| `privacy.inkingTypingOff` | Inking and typing personalization off | safe, preview |  | registry | |
| `privacy.onlineSpeechOff` | Online speech recognition off | safe |  | registry | |
| `privacy.webSearchOff` | Web results in Start search off | safe |  | registry | |
| `privacy.searchHighlightsOff` | Search highlights off | safe |  | registry | |
| `privacy.cloudSearchOff` | Cloud content in search off | safe |  | registry | |
| `privacy.cloudClipboardOff` | Clipboard sync across devices off | safe |  | registry | |
| `privacy.suggestionsOff` | Tips, suggestions and welcome screens off | safe |  | registry | |
| `privacy.onlineTipsOff` | Online tips in Settings off | safe |  | registry | |
| `privacy.appLaunchTrackingOff` | App launch tracking off | safe |  | registry | |
| `privacy.languageListOff` | Language list for websites off | safe |  | registry | |
| `privacy.findMyDeviceOff` | Find my device off | moderate |  | registry | |
| `privacy.errorReportingOff` | Windows Error Reporting off | moderate |  | registry | |
| `privacy.ceipOff` | Customer Experience Improvement Program off | safe |  | registry | |
| `privacy.appPersonalDataOff` | Store app access to personal data off | moderate |  | registry | |
| `privacy.settingsSyncOff` | Settings sync off | moderate |  | registry | |
| `privacy.messageSyncOff` | Text message cloud backup off | safe |  | registry | |
| `privacy.mapsTrafficOff` | Offline maps updates off | safe |  | registry | |
| `privacy.clipboardHistoryOff` | Clipboard history off | moderate |  | registry | |
| `services.gamingPreset` | Conservative services preset | safe |  | service | |
| `battery.boostOffDc` | Processor boost off on battery | moderate |  | powerSetting | |
| `battery.wifiPowerSavingDc` | Wi-Fi power saving on battery: maximum | safe |  | powerSetting | |
| `battery.pcieAspmMaxDc` | PCIe power saving on battery: maximum | safe |  | powerSetting | |
| `quiet.boostOff` | Processor boost off (quieter and cooler) | moderate |  | powerSetting | |
| `quiet.powerModeEfficiency` | Power mode: Best power efficiency | safe |  | powerMode | |
| `office.clipboardHistoryOn` | Clipboard history on | safe |  | registry | |
| `office.launchToThisPc` | File Explorer opens to This PC | safe |  | registry | |
| `background.edgeBoostOff` | Microsoft Edge: no startup boost, no background mode | safe |  | registry | |
| `updates.driversExcluded` | Drivers not included with Windows Update | moderate |  | registry | |
| `privacy.deviceMetadataOff` | No automatic download of device apps | safe |  | registry | |
| `edge.diagnosticDataRequired` | Microsoft Edge: only required diagnostic data | safe |  | registry | |
| `edge.personalizationOff` | Microsoft Edge: no browsing data for personalization | safe |  | registry | |
| `edge.shoppingOff` | Microsoft Edge: no shopping features | safe |  | registry | |
| `edge.sidebarOff` | Microsoft Edge: no sidebar | safe |  | registry | |
| `edge.setupPromptsOff` | Microsoft Edge: no setup and default browser prompts | safe |  | registry | |
| `edge.newTabQuiet` | Microsoft Edge: new tab page without news and promotions | safe |  | registry | |
| `edge.quickLinksOff` | Microsoft Edge: no quick links on the new tab page | safe |  | registry | |
| `edge.promotionsOff` | Microsoft Edge: no tips, recommendations and Acrobat offer | safe |  | registry | |
| `edge.trackingPreventionStrict` | Microsoft Edge: strict tracking prevention | moderate |  | registry | |
| `edge.searchSuggestionsOff` | Microsoft Edge: no search and site suggestions while typing | safe |  | registry | |
| `edge.localSuggestionsOff` | Microsoft Edge: no suggestions from history and favorites | safe |  | registry | |
| `edge.errorPageServicesOff` | Microsoft Edge: no web services for error pages | safe |  | registry | |
| `edge.networkPredictionOff` | Microsoft Edge: no network prediction | safe |  | registry | |
| `edge.feedbackOff` | Microsoft Edge: no feedback tool | safe |  | registry | |
| `edge.paymentQueryOff` | Microsoft Edge: sites cannot check for saved payment methods | safe |  | registry | |
| `edge.passwordSavingOff` | Microsoft Edge: no saving of passwords | safe |  | registry | |
| `edge.signInOff` | Microsoft Edge: no browser sign-in | moderate |  | registry | |
| `edge.aiOff` | Microsoft Edge: generative AI features off | safe |  | registry | |
| `edge.cloudWritingOff` | Microsoft Edge: no cloud text prediction and spell checking | safe |  | registry | |
| `edge.tabServicesOff` | Microsoft Edge: no tab organization service | safe |  | registry | |
| `edge.visualSearchOff` | Microsoft Edge: no visual search on images | safe |  | registry | |
| `chrome.aiOff` | Google Chrome: generative AI features off | safe |  | registry | |
| `chrome.aiNoTraining` | Google Chrome: AI features without improving Google's models | safe |  | registry | |
| `chrome.urlDataOff` | Google Chrome: no URL-keyed data collection | safe |  | registry | |
| `chrome.promotionsOff` | Google Chrome: no promotions and price tracking | safe |  | registry | |
| `chrome.backgroundOff` | Google Chrome: no background mode | safe |  | registry | |
| `brave.cryptoOff` | Brave: no Rewards, Wallet and VPN | safe |  | registry | |
| `brave.aiChatOff` | Brave: no Leo AI assistant | safe |  | registry | |
| `brave.telemetryOff` | Brave: no product analytics, usage ping and Web Discovery | safe |  | registry | |
| `brave.extrasOff` | Brave: no News, Talk and Playlist | safe |  | registry | |
| `ai.paintOff` | Paint: Cocreator, generative fill and Image Creator off | safe |  | registry | |
| `ai.notepadOff` | Notepad: AI features off | safe |  | registry | |
| `perm.cameraOff` | Windows apps: no camera access | safe |  | registry | |
| `perm.microphoneOff` | Windows apps: no microphone access | safe |  | registry | |
| `perm.notificationsOff` | Windows apps: no access to your notifications | safe |  | registry | |
| `perm.voiceActivationOff` | Windows apps: no voice activation | safe |  | registry | |
| `perm.motionOff` | Windows apps: no motion data | safe |  | registry | |
| `perm.phoneOff` | Windows apps: no phone calls | safe |  | registry | |
| `perm.radiosOff` | Windows apps: no control of radios | safe |  | registry | |
| `perm.devicesOff` | Windows apps: no unpaired or trusted devices | safe |  | registry | |
| `privacy.locationServiceOff` | Location service off for everything | safe |  | registry | |
| `privacy.searchConnectedOff` | Windows Search: no web results and no location | safe |  | registry | |
| `privacy.malwareReportOff` | Malicious Software Removal Tool: no infection reports | safe |  | registry | |
| `privacy.thirdPartySpotlightOff` | Windows Spotlight: no third-party suggestions | safe |  | registry | |
| `privacy.spotlightOff` | Windows Spotlight off | safe |  | registry | |
| `privacy.cloudContentOff` | No cloud content, account notices and Windows tips | safe |  | registry | |
| `privacy.oneSettingsOff` | No configuration downloads from OneSettings | moderate |  | registry | |
| `privacy.privacyExperienceOff` | No privacy settings page at sign-in | safe |  | registry | |
| `privacy.recentFilesOff` | No recently opened files in Start, jump lists and File Explorer | safe |  | registry | |
| `privacy.lockScreenCameraOff` | No camera on the lock screen | safe |  | registry | |
| `privacy.drmOnlineOff` | Windows Media DRM: no internet access | safe |  | registry | |
| `privacy.oneDriveFolderBackupOff` | OneDrive: no backup of Desktop, Documents and Pictures | safe |  | registry | |
| `background.oneDriveSyncOff` | OneDrive file sync off | moderate |  | registry | |
| `updates.featureUpdatesDeferred` | Feature updates one year later | safe |  | registry | |
| `updates.storeAutoUpdateOff` | Microsoft Store: no automatic app updates | safe |  | registry | |
| `storage.appArchivingOff` | No automatic archiving of unused apps | safe |  | registry | |
| `focus.updateNotificationsReduced` | Fewer Windows Update notifications | safe |  | registry | |
| `focus.toastsOff` | No pop-up notifications from apps | safe |  | registry | |
| `focus.lockScreenToastsOff` | No app notifications on the lock screen | safe |  | registry | |
| `power.wakeTimersOff` | No wake timers | safe |  | powerSetting | |
| `personalize.logonBlurOff` | Sign-in screen: clear background | safe |  | registry | |
| `personalize.firstLogonAnimationOff` | No first sign-in animation | safe |  | registry | |
| `system.verboseStatus` | Detailed status messages at startup and shutdown | safe |  | registry | |
| `personalize.hibernateInPowerMenu` | Hibernate in the power menu | safe |  | registry | |
| `explorer.openWithPromptsOff` | Open with: no Store search and no new app notices | safe |  | registry | |
| `explorer.shortcutSearchOff` | Broken shortcuts: no drive search | safe |  | registry | |
| `personalize.inkWorkspaceOff` | Windows Ink Workspace off | safe |  | registry | |
| `personalize.defaultPrinterManual` | Default printer stays as you set it | safe |  | registry | |
| `input.accessibilityShortcutsOff` | No Sticky Keys, Filter Keys and Toggle Keys shortcuts | safe |  | accessibilityShortcut | |
| `security.puaProtection` | Microsoft Defender: block potentially unwanted apps | safe |  | registry | |
| `security.networkProtection` | Microsoft Defender: network protection | moderate |  | registry | |
| `security.passwordRevealOff` | No button to reveal passwords | safe |  | registry | |
| `security.llmnrOff` | LLMNR off | moderate |  | registry | |
| `security.autoRestartSignOnOff` | No automatic sign-in after restarts | safe |  | registry | |
| `security.remoteAssistanceOff` | Remote Assistance requests off | safe |  | registry | |
| `security.projectionToPcOff` | No wireless projection to this PC | safe |  | registry | |
| `security.autoPlayOff` | AutoPlay off on all drives | safe |  | registry | |
| `security.deviceEncryptionPrevented` | No automatic device encryption | moderate |  | registry | |
| `background.pcaOff` | Program Compatibility Assistant off | safe |  | registry | |
| `network.smbThrottlingOff` | Network file transfers: no SMB throttling | safe |  | registry | |
| `battery.noIndexingOnBattery` | No search indexing on battery | safe |  | registry | |
| `network.dohAutoUpgrade` | DNS over HTTPS for Cloudflare, Google and Quad9 | moderate, preview |  | dohAutoUpgrade | |
| `storage.reservedStorageOff` | Reserved storage off | moderate |  | reservedStorage | |
| `personalize.darkMode` | Dark mode for Windows and apps | safe, preview |  | registry | |
| `personalize.accentTitleBars` | Accent color on title bars | safe, preview |  | registry | |
| `explorer.hiddenFiles` | File Explorer: show hidden files | safe, preview |  | registry | |
| `personalize.taskbarLeft` | Taskbar: icons on the left | safe, preview |  | registry | |
| `personalize.clockSeconds` | Taskbar clock with seconds | safe, preview |  | registry | |
| `personalize.taskbarNeverCombine` | Taskbar: never combine buttons | safe, preview |  | registry | |
| `focus.taskbarFlashingOff` | Taskbar: no flashing buttons | safe, preview |  | registry | |
| `focus.taskbarBadgesOff` | Taskbar: no badges on apps | safe, preview |  | registry | |
| `personalize.showDesktopCornerOff` | Taskbar: no show desktop corner | safe, preview |  | registry | |
| `personalize.snapAssistOff` | Snap: no suggestions for the other half | safe, preview |  | registry | |
| `personalize.snapLayoutsOff` | Snap: no layouts on maximize button and screen top | safe, preview |  | registry | |
| `explorer.compactView` | File Explorer: compact view | safe, preview |  | registry | |
| `explorer.itemCheckboxes` | File Explorer: item check boxes | safe, preview |  | registry | |
| `focus.notificationSoundsOff` | Notifications without sound | safe, preview |  | registry | |
| `personalize.dynamicLightingOff` | Dynamic Lighting off | safe, preview |  | registry | |
| `personalize.communicationsDuckingOff` | Game audio stays loud during voice calls | safe, preview |  | registry | |
| `display.autoHdrOn` | Auto HDR on | safe, preview |  | registryToken | |
| `focus.startAccountNotificationsOff` | Start: no account notifications | safe |  | registry | |
| `focus.altTabTabsOff` | Alt+Tab: open windows only | safe, preview |  | registry | |
| `privacy.searchHistoryOff` | Search: no history on this device | safe, preview |  | registry | |
| `focus.lockScreenTipsOff` | Lock screen: no fun facts and tips | safe, preview |  | registry | |
| `focus.finishSetupOff` | No "Let's finish setting up your device" screen | safe, preview |  | registry | |
| `explorer.syncProviderNotificationsOff` | File Explorer: no sync provider notifications | safe, preview |  | registry | |
| `display.vrrOn` | Variable refresh rate for older full screen games | safe, preview |  | registryToken | |

## 5. Tweaks that need real hardware

A VM has no NVIDIA GPU and only a synthetic network adapter. Test these on a spare real PC with the hardware named, one at a time, with a restore point first.

| Tweak | Title | Risk | Needs | Actions | Result |
|---|---|---|---|---|---|
| `nvidia.lowLatencyOn` | NVIDIA Low Latency Mode: On | safe, preview | NVIDIA GPU | nvidiaDrs | |
| `nvidia.shaderCacheUnlimited` | NVIDIA shader cache: unlimited | safe, preview | NVIDIA GPU | nvidiaDrs | |
| `nvidia.textureFilteringPerformance` | NVIDIA texture filtering: high performance | safe, preview | NVIDIA GPU | nvidiaDrs | |
| `nvidia.preferMaxPerformance` | NVIDIA power mode: prefer maximum performance (global) | moderate, preview | NVIDIA GPU | nvidiaDrs | |
| `network.nicPowerSavingOff` | Network adapter power saving off | moderate, preview | physical network adapter | nicProperty | |
| `network.nicAllowPowerOffOff` | Do not let Windows turn off the network adapter | moderate, preview | physical network adapter | nicProperty | |
| `network.interruptModerationOff` | Interrupt moderation off (Expert) | expert, preview | physical network adapter | nicProperty | |

### Undocumented values: check the effect

These tweaks use values Microsoft does not document and stay previews until a test shows that the value does what the page says. Check the visible effect after apply and after undo, then add the result to the tweak's `proof` in the catalog.

| Tweak | Title | Check |
|---|---|---|
| `gpu.hags` | Hardware-accelerated GPU scheduling (HAGS) | Settings > System > Display > Graphics shows the switch on after the restart (needs a GPU with HAGS support) |
| `gpu.gameDvrOff` | Game Bar captures off | Settings > Gaming > Captures shows background recording off; Win+Alt+R records nothing |
| `privacy.inkingTypingOff` | Inking and typing personalization off | Settings > Privacy & security > Inking & typing personalization shows both switches off |
| `personalize.darkMode` | Dark mode for Windows and apps | the effect the explanation page describes |
| `personalize.accentTitleBars` | Accent color on title bars | the effect the explanation page describes |
| `explorer.hiddenFiles` | File Explorer: show hidden files | the effect the explanation page describes |
| `personalize.taskbarLeft` | Taskbar: icons on the left | the effect the explanation page describes |
| `personalize.clockSeconds` | Taskbar clock with seconds | the effect the explanation page describes |
| `personalize.taskbarNeverCombine` | Taskbar: never combine buttons | the effect the explanation page describes |
| `focus.taskbarFlashingOff` | Taskbar: no flashing buttons | the effect the explanation page describes |
| `focus.taskbarBadgesOff` | Taskbar: no badges on apps | the effect the explanation page describes |
| `personalize.showDesktopCornerOff` | Taskbar: no show desktop corner | the effect the explanation page describes |
| `personalize.snapAssistOff` | Snap: no suggestions for the other half | the effect the explanation page describes |
| `personalize.snapLayoutsOff` | Snap: no layouts on maximize button and screen top | the effect the explanation page describes |
| `explorer.compactView` | File Explorer: compact view | the effect the explanation page describes |
| `explorer.itemCheckboxes` | File Explorer: item check boxes | the effect the explanation page describes |
| `focus.notificationSoundsOff` | Notifications without sound | the effect the explanation page describes |
| `personalize.dynamicLightingOff` | Dynamic Lighting off | the effect the explanation page describes |
| `personalize.communicationsDuckingOff` | Game audio stays loud during voice calls | the effect the explanation page describes |
| `personalize.desktopThisPc` | This PC icon on the desktop | the effect the explanation page describes |
| `display.autoHdrOn` | Auto HDR on | the effect the explanation page describes |
| `privacy.searchHistoryOff` | Search: no history on this device | the effect the explanation page describes |
| `input.printScreenSnippingOff` | Print Screen copies the screen again | the effect the explanation page describes |
| `focus.lockScreenTipsOff` | Lock screen: no fun facts and tips | the effect the explanation page describes |
| `focus.finishSetupOff` | No "Let's finish setting up your device" screen | the effect the explanation page describes |
| `explorer.syncProviderNotificationsOff` | File Explorer: no sync provider notifications | the effect the explanation page describes |
| `explorer.fullPathTitle` | File Explorer: full path in the title | the effect the explanation page describes |
| `display.vrrOn` | Variable refresh rate for older full screen games | the effect the explanation page describes |

<!-- End of generated tables. -->

## 6. Changes built at runtime and other features that change the PC

| Feature | Test | Result |
|---|---|---|
| Per-game NVIDIA profiles (real NVIDIA PC) | Create a profile for one game, check in NVIDIA Control Panel, undo | |
| MSI mode and interrupt affinity (Expert, real PC) | Change one device, restart, device works, undo, restart | |
| Startup entries | Disable one Run key entry and one Startup folder item, sign out and in: not started; enable again | |
| Services | Change one explained service to Manual, restart, undo | |
| Scheduled tasks | Turn one task off and on again | |
| Windows features | Turn one optional feature on, restart, turn it off again | |
| Debloat | Remove two inbox apps, check they are gone for a new user too, reinstall from the Store link | |
| OneDrive | With Known Folder Move on: uninstall is blocked; without: uninstall works | |
| Cleanup | Each of the 11 categories: shown size matches, files are gone afterwards, files in use are skipped, only the signed-in user's Recycle Bin is emptied | |
| Storage analyzer | Delete one duplicate: it goes to the Recycle Bin; Windows and program folders cannot be selected | |
| Apps | Install one per-user app (runs as the signed-in user) and one machine-wide app with winget | |
| DNS presets | Apply Cloudflare, check `Get-DnsClientServerAddress`, undo: DHCP or the old servers are back | |
| DNS benchmark | Runs, shows results, changes nothing | |
| Tools | Winsock reset (restart needed), Explorer restart, DNS flush, Windows Update repair | |
| Health | SFC and DISM with live output; frame time benchmark with PresentMon; throttle check; PawnIO install from the Health page and uninstall with winget | |
