# VM test plan

Real apply and undo round trips for every change the app can make. Unit tests only cover the registry sandbox and fakes; this plan covers real Windows. Run it before each public release, in a Hyper-V VM, never on your own PC.

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

The tables below cover all 86 catalog tweaks. Preview tweaks have not been tested on real Windows yet; test them first.

## 2. Expert and boot-critical tweaks

Turn on Expert mode. Take a checkpoint before **each** of these, apply it alone, restart, confirm Windows still starts, then undo and restart again.

| Tweak | Title | Risk | Then | Actions | Result |
|---|---|---|---|---|---|
| `security.vbsOff` | Virtualization-based security and memory integrity off | expert, boot-critical, anti-cheat sensitive, preview | restart | registry | |
| `leftover.usePlatformClock` | Remove forced platform clock (useplatformclock) | expert, boot-critical, preview | restart | bcd | |

## 3. Tweaks that need a restart or sign-out

| Tweak | Title | Risk | Then | Actions | Result |
|---|---|---|---|---|---|
| `power.throttlingOff` | Power throttling off | safe | restart | registry | |
| `gpu.hags` | Hardware-accelerated GPU scheduling (HAGS) | safe | restart | registry | |
| `gpu.mpoOff` | Multiplane overlay (MPO) off | moderate, preview | restart | registry | |
| `memory.compressionOff` | Memory compression off | moderate, preview | restart | memoryCompression | |
| `memory.sysmainOff` | SysMain (Superfetch) off | moderate | restart | service | |
| `memory.pagefileSystemManaged` | Page file managed by Windows | safe, preview | restart | registry | |
| `storage.lastAccessOff` | NTFS last-access timestamps off | safe | restart | registry | |
| `network.throttlingIndex` | Network throttling off (NetworkThrottlingIndex) | safe | restart | registry | |
| `network.preferIpv4` | Prefer IPv4 over IPv6 | safe | restart | registryBits | |
| `privacy.telemetryOff` | Telemetry to minimum | moderate | restart | registry, service, scheduledTask | |
| `visual.bestPerformance` | Visual effects: best performance | safe, preview | sign out | registry, registryBinaryBits | |
| `explorer.classicContextMenu` | Classic right-click menu | safe, preview | sign out | registry | |
| `privacy.appCompatTelemetryOff` | Application compatibility telemetry off | moderate | restart | registry | |
| `privacy.phoneLinkOff` | Phone-PC linking off | moderate | restart | registry | |
| `privacy.crossDeviceOff` | Continue experiences on other devices off | moderate | restart | registry | |
| `system.registryBackup` | Registry backup to the RegBack folder | safe | restart | registry | |

## 4. Other tweaks

| Tweak | Title | Risk | Then | Actions | Result |
|---|---|---|---|---|---|
| `power.balancedPlan` | Switch to the Balanced power plan | safe |  | powerScheme | |
| `power.gamingPlan` | Gaming power plan (based on High performance) | safe |  | powerScheme | |
| `power.ultimatePlan` | Ultimate Performance power plan | moderate, preview |  | powerScheme | |
| `power.turboRestore` | Restore processor turbo | safe |  | powerSetting | |
| `power.usbSelectiveSuspendOff` | USB selective suspend off | safe |  | powerSetting | |
| `power.pcieAspmOff` | PCIe link power management off | safe |  | powerSetting | |
| `power.fastStartupOff` | Fast Startup off | safe |  | registry | |
| `power.hibernateOff` | Hibernation off | safe |  | hibernation | |
| `gpu.windowedOptimizations` | Optimizations for windowed games | safe |  | registryToken | |
| `gpu.gameMode` | Game Mode on | safe |  | registry | |
| `gpu.gameDvrOff` | Game Bar captures off | safe |  | registry | |
| `input.mouseAccelOff` | Mouse acceleration off | safe |  | registry | |
| `storage.trimOn` | TRIM on | safe |  | registry | |
| `storage.storageSenseOn` | Storage Sense on | safe, preview |  | registry | |
| `network.nagleOff` | Delayed TCP acknowledgements off (TcpAckFrequency) | moderate |  | registry | |
| `network.deliveryOptimizationP2POff` | Delivery Optimization peer-to-peer off | safe |  | registry | |
| `privacy.activityHistoryOff` | Activity history off | safe |  | registry | |
| `privacy.consumerFeaturesOff` | Consumer features off | safe |  | registry | |
| `privacy.locationOff` | Location access off | safe |  | registry | |
| `background.backgroundAppsOff` | Background apps off | moderate |  | registry | |
| `background.aiOff` | Recall snapshots off | safe |  | registry | |
| `background.widgetsOff` | Widgets off | safe |  | registry | |
| `visual.transparencyOff` | Transparency effects off | safe |  | registry | |
| `explorer.fileExtensions` | Show file extensions | safe |  | registry | |
| `explorer.endTask` | "End task" in the taskbar menu | safe |  | registry | |
| `network.dns.cloudflare` | Public DNS servers | safe, preview |  | dns | |
| `network.dns.google` | Public DNS servers | safe, preview |  | dns | |
| `network.dns.quad9` | Public DNS servers | safe, preview |  | dns | |
| `privacy.advertisingIdOff` | Advertising ID off | safe |  | registry | |
| `privacy.tailoredExperiencesOff` | Tailored experiences off | safe |  | registry | |
| `privacy.feedbackNotificationsOff` | Feedback requests off | safe |  | registry | |
| `privacy.diagnosticLogsLimited` | Limit diagnostic logs and memory dumps | safe |  | registry | |
| `privacy.inkingTypingOff` | Inking and typing personalization off | safe |  | registry | |
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
| `services.gamingPreset` | Conservative services preset | safe, preview |  | service | |
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
