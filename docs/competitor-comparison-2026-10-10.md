# Feature comparison with WinUtil and O&O ShutUp10++, 2026-10-10

Scope: every feature of Chris Titus Tech's Windows Utility (WinUtil) and O&O ShutUp10++ that PCOptimizer 0.4.0 (commit 20650b3) does not have, with a verdict on whether it can be built under the rules in CLAUDE.md, and why not where it cannot. Read only; no code was changed.

## Method and limits

- **PCOptimizer:** read from the catalog (`src/Optimizer.Core/Catalog/Tweaks/*.json`, `Catalog/Data/*.json`), the engine folders (Cleanup, Tools, Apps, Network, Debloat, Backup), the views and `Strings.resx`, the explanation pages, README, CONTRIBUTING and `docs/not-included.md`.
- **WinUtil:** the `main` branch on 2026-10-10. Parsed all 67 entries of `config/tweaks.json`, all 33 of `feature.json`, and `applications.json` (236 apps), `appx.json`, `appnavigation.json`, `dns.json` and `preset.json`. Read the scripts in `functions/public` and `functions/private` that implement actions (DNS, update policies, system repair, ISO builder, import and export, headless run, autologon, network reset, WinGet repair). Registry values and commands quoted below come from those files.
- **O&O ShutUp10++:** version 3.6.1135 (release date 2026-09-29 on the product page). O&O does not publish the full list of its nearly 300 settings. This document uses the manual (web and PDF, 116 pages): the 22 categories with their example settings, the table of controls added in 3.2 to 3.4 (with IDs), the feature comparison, AI removal, edit mode, undo history, profiles, the settings dialog and the FAQ. Settings that exist in the app but are not named in the manual are not listed one by one; they fall under their category row.
- **Documentation status:** a second pass opened the Microsoft pages behind the verdicts that depend on editions or deprecation (Policy CSP WindowsAI, Experience, Start, System and Update; tamper protection; deprecated features; PowerShell about_Telemetry). Those facts are marked "(checked)". Everything else marked "documented" is still a first assessment: before an item is built, its sources must be opened as CLAUDE.md requires, and an item without a supporting source becomes "No" or `verified: false`.
- **Editions:** Policy CSP pages list Pro, Enterprise and Education, never Home. PCOptimizer already hides Group Policy tweaks on Home; every policy based item below inherits that.

## Verdicts

| Verdict | Meaning |
|---|---|
| **Yes** | Fits the rules: documented setting, reversible, honest impact. Can go into the catalog or a page. |
| **Conditional** | Buildable with a restriction: Expert only, edition gated, opt-in only, only when an app is installed, needs a source check or a VM test first, or needs your decision under the scope rule. |
| **No** | Should not be built. The reason column says why: undocumented, security loss, hack, removed or deprecated in Windows 11, against a CLAUDE.md rule, or out of scope. |
| **Covered** | PCOptimizer already has it (section 1). |

## Summary

- WinUtil: 69 features missing in PCOptimizer (section 2): 23 Yes, 22 Conditional, 24 No. Items already rejected in `not-included.md` and covered items are not counted.
- O&O ShutUp10++: 19 app features (7 Yes, 11 Conditional, 1 No) and 25 setting groups (9 Yes, 10 Conditional, 6 No) missing, plus one group of settings for features that Windows 11 no longer has (section 3).
- Most of the O&O gap is privacy breadth (app permissions, Office, Edge, lock screen, AI policies). Most of the WinUtil gap is convenience (app catalog size, uninstall and upgrade all, export and import, DNS over HTTPS, Windows Update presets) and personalization toggles.
- Several policies that both competitors write only work on Enterprise and Education (Spotlight, consumer features, Settings agent, some Recall controls). PCOptimizer should keep gating these by edition instead of writing them everywhere.
- Things both tools do that PCOptimizer refuses on purpose are listed in section 4 and are already explained in `docs/not-included.md`.

---

## 1. Already covered by PCOptimizer

| Competitor feature | PCOptimizer equivalent | Difference |
|---|---|---|
| WinUtil Activity History off; O&O activity history | `privacy.activityHistoryOff` | none |
| WinUtil Hibernation off | `power.hibernateOff` | WinUtil also hides the Hibernate menu entry (`FlyoutMenuSettings\ShowHibernateOption=0`) |
| WinUtil Widgets remove; O&O widgets, news and interests | `background.widgetsOff` (policy `Dsh\AllowNewsAndInterests`) | WinUtil uninstalls `Microsoft.WidgetsPlatformRuntime` and `MicrosoftWindows.Client.WebExperience`; PCOptimizer uses the reversible policy |
| WinUtil Location tracking off; O&O app location access | `privacy.locationOff` | WinUtil also sets `Sensor\Overrides\{BFA794E4-...}\SensorPermissionState=0`, `HKLM\SYSTEM\Maps\AutoUpdateEnabled=0` and disables `lfsvc` (see 3.2) |
| WinUtil ConsumerFeatures off; O&O Microsoft consumer features (P097) | `privacy.consumerFeaturesOff` | PCOptimizer offers it only on Enterprise and Education, because Microsoft lists `AllowWindowsConsumerFeatures` for those editions only (checked). WinUtil writes it on every edition, where it may do nothing |
| WinUtil Telemetry off; O&O telemetry category | `privacy.telemetryOff`, `privacy.advertisingIdOff`, `privacy.tailoredExperiencesOff`, `privacy.inkingTypingOff`, `privacy.onlineSpeechOff`, `privacy.appLaunchTrackingOff`, `privacy.feedbackNotificationsOff`, `privacy.errorReportingOff` | WinUtil writes `AllowTelemetry=0` under `CurrentVersion\Policies\DataCollection`; the value 0 is only honored on Enterprise and Education (checked), which PCOptimizer's page already says. WinUtil also disables `wermgr`, sets Defender `SubmitSamplesConsent=2` and sets `POWERSHELL_TELEMETRY_OPTOUT=1` (see 2.1) |
| WinUtil Delivery Optimization off; O&O Windows Update peer to peer | `network.deliveryOptimizationP2POff` | none |
| WinUtil Restore point create | restore point once per session, `system.restorePointFrequency` | none |
| WinUtil End Task in taskbar | `explorer.endTask` | none |
| WinUtil Storage Sense off | `storage.storageSenseOn` | opposite direction on purpose: PCOptimizer recommends turning it on |
| WinUtil IPv4 preferred | `network.preferIpv4` | none |
| WinUtil Background apps off | `background.backgroundAppsOff` | none |
| WinUtil Visual effects best performance | `visual.bestPerformance` | WinUtil also hides Task View, Chat and the search box, turns off Aero Peek and sets `KeyboardDelay=0` in the same tweak (see 2.2) |
| WinUtil Classic right-click menu | `explorer.classicContextMenu` | none |
| WinUtil File extensions | `explorer.fileExtensions` | none |
| WinUtil Mouse acceleration | `input.mouseAccelOff` | none |
| WinUtil Game Mode | `gpu.gameMode` | none |
| WinUtil Multiplane overlay, "Disabled (Compatibility)" | `gpu.mpoOff` (`OverlayTestMode=5`) | WinUtil also has "Fully Disabled" (see 2.1) |
| WinUtil Ultimate Performance plan add and remove | `power.ultimatePlan`, `power.gamingPlan`, Undo | none |
| WinUtil DNS presets (Google, Cloudflare, Quad9) and DNS benchmark | `network.dns.*`, DNS benchmark on the Network page | WinUtil has more presets, IPv6 addresses, DNS over HTTPS and "Fastest" (see 2.3). PCOptimizer's benchmark sends a real DNS query over UDP; WinUtil only times a TCP connect to port 53 |
| WinUtil OneDrive remove | Debloat page OneDrive uninstall | WinUtil also deletes leftovers and disables `OneSyncSvc` |
| WinUtil AppX removal | Debloat page (34 apps) | WinUtil also offers Paint, Notepad, Snipping Tool, Calculator and Photos, which PCOptimizer protects on purpose, plus Cross Device Experience Host and Start Experiences App (see 2.1). PCOptimizer additionally offers People, Tips, Copilot provider, Cortana, Mail and Calendar, Maps and classic Teams |
| WinUtil features: .NET 3.5 and 4 advanced services, Hyper-V, legacy media, WSL, Sandbox | `features.json`: NetFx3, Hyper-V, DirectPlay, WindowsMediaPlayer, WSL, Containers-DisposableClientVM | NFS is missing (see 2.3); WinUtil also enables `NetFx4-AdvSrvs`, `MediaPlayback` and `LegacyComponents` |
| WinUtil Registry backup | `system.registryBackup` | WinUtil also creates a daily task |
| WinUtil Windows Update reset | Tools: Update repair | none |
| WinUtil System corruption scan | Health: SFC, DISM ScanHealth and RestoreHealth | WinUtil runs `chkdsk /scan /perf` first (see 2.3) |
| WinUtil Network reset (part) | Tools quick action: Winsock reset | `netsh int ip reset` is missing (see 2.3) |
| WinUtil Temp files, Disk cleanup | Cleanup page (11 categories), DISM component cleanup | WinUtil uses `/ResetBase` (rejected, section 4); Windows.old is missing (see 2.1) |
| WinUtil Device companion apps off | `privacy.deviceMetadataOff` | Microsoft deprecated device metadata in May 2025 (checked); the policy still applies while the feature exists |
| WinUtil Updates: driver exclusion; O&O W010 | `updates.driversExcluded` | none |
| WinUtil Bing search toggle; O&O web search in Start | `privacy.webSearchOff`, `privacy.cloudSearchOff`, `privacy.searchHighlightsOff` | none |
| WinUtil Edge (part) | `background.edgeBoostOff` | the other Edge policies are missing (see 2.1) |
| WinUtil Environment report | `--report`, Settings: report a problem, copy log | none |
| WinUtil "Select installed tweaks", live toggle state | Tweaks page shows the applied state of every tweak | none |
| WinUtil Undo all | Changes page: Undo all | none |
| WinUtil Show installed apps | Apps page shows installed state | none |
| WinUtil Presets (Standard, Minimal, Advanced) | Profiles and "Apply recommended" | PCOptimizer gives a reason per item |
| O&O clipboard history, cloud clipboard | `privacy.clipboardHistoryOff`, `privacy.cloudClipboardOff` | none |
| O&O sync of all settings | `privacy.settingsSyncOff` | O&O can also turn off only credentials, language or design; PCOptimizer turns off all |
| O&O Phone Link, PC to mobile | `privacy.phoneLinkOff`, `privacy.crossDeviceOff` | none |
| O&O tips and suggestions, Start app suggestions, Settings suggestions | `privacy.suggestionsOff`, `privacy.onlineTipsOff` | none |
| O&O Find My Device (L008) | `privacy.findMyDeviceOff` | none |
| O&O CEIP | `privacy.ceipOff` | none |
| O&O app access to account info, contacts, calendar, call history, email, messaging, tasks, diagnostics | `privacy.appPersonalDataOff` | the other app permissions are missing (see 3.2) |
| O&O diagnostic log collection | `privacy.diagnosticLogsLimited` | none |
| O&O Xbox Game Bar and Game DVR | `gpu.gameDvrOff` | none |
| O&O recommendation levels | impact 0 to 5, basis, risk, Expert flag | PCOptimizer gives more detail |
| O&O write verification | verify after apply | none |
| O&O availability per Windows version and edition | edition filtering of policy tweaks | none |
| O&O restore point prompt | restore point once per session | none |
| O&O "Apply only recommended settings", first run setup | "Apply recommended" on the Overview page | none |
| O&O Premium: detect settings reset by Windows updates | drift banner after Windows updates, "Apply again" | O&O Premium re-applies automatically (see 3.1) |
| O&O app themes System, Light, Dark | Settings: theme | O&O also has high contrast themes (see 3.1) |
| O&O AI removal, layers 1 and 2 | `background.aiOff` (`DisableAIDataAnalysis`), Copilot app removal in Debloat, Recall in `features.json` | the other WindowsAI policies and Recall data deletion are missing (see 2.1 and 3.1) |

---

## 2. Missing from Chris Titus WinUtil

### 2.1 Essential and advanced tweaks

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| Edge debloat | 17 Edge policies under `HKLM\SOFTWARE\Policies\Microsoft\Edge` and `EdgeUpdate`: `PersonalizationReportingEnabled=0`, `ShowRecommendationsEnabled=0`, `HideFirstRunExperience=1`, `UserFeedbackAllowed=0`, `ConfigureDoNotTrack=1`, `AlternateErrorPagesEnabled=0`, `EdgeCollectionsEnabled=0`, `EdgeShoppingAssistantEnabled=0`, `MicrosoftEdgeInsiderPromotionEnabled=0`, `ShowMicrosoftRewards=0`, `WebWidgetAllowed=0`, `DiagnosticData=0`, `EdgeAssetDeliveryServiceEnabled=0`, `WalletDonationEnabled=0`, `DefaultBrowserSettingsCampaignEnabled=0`, `CreateDesktopShortcutDefault=0`, and an extension block list entry | Yes | Microsoft documents the Edge policies in the Edge policy reference. Some policies only take effect on managed (domain joined or MDM) devices; each one needs a check | One catalog group "Edge privacy" next to `background.edgeBoostOff`, impact 0, shown only when Edge is installed. Split into separate tweaks so a user can keep Collections or Rewards. Leave out the extension block list (it blocks one extension ID without a stated reason) |
| Brave debloat | 12 policies under `HKLM\SOFTWARE\Policies\BraveSoftware\Brave`: Rewards, Wallet, VPN, Leo AI, stats ping, News, Talk, Tor, P3A, URL keyed data, Safe Browsing extended reporting, metrics | Conditional | Brave documents its group policies; needs a source check per value. No gaming impact | Opt-in group, shown only when Brave is installed, not in any profile recommendation |
| Reserved Storage off | `DISM /Online /Set-ReservedStorageState /State:Disabled` | Yes | Documented DISM option, reversible with `/State:Enabled` | Tweak for the Older PC profile on drives under a size threshold, with a warning that feature updates may need the space. Process runner action with undo |
| Windows AI: policies | `SettingsPageVisibility=hide:aicomponents`, `HKLM\SOFTWARE\Policies\WindowsNotepad\DisableAIFeatures=1` | Yes for Paint, Copilot key and Recall enablement; Conditional for the rest | Checked in Policy CSP WindowsAI: `DisableImageCreator`, `DisableCocreator`, `DisableGenerativeFill` (Pro and higher, Windows 11 22H2 and 24H2 builds named there), `SetCopilotHardwareKey` (Pro and higher, user scope), `AllowRecallEnablement` (Pro and higher, 24H2 with KB5055627; disabling it removes the Recall bits and deletes saved snapshots). `DisableClickToDo` is Pro and higher but listed for Insider builds only. `DisableSettingsAgent` and `RemoveMicrosoftCopilotApp` are Enterprise and Education only. The Notepad value and hiding a Settings page still need a source | Separate tweaks per AI feature next to `background.aiOff`, edition and build gated (see also 3.2 Copilot and Windows AI) |
| Windows AI: removal | Marks `MicrosoftWindows.Client.CoreAI` end of life in `AppxAllUserStore\EndOfLife`, removes all `*Copilot*` packages and the Microsoft 365 hub, runs `winget uninstall Copilot`, disables `WSAIFabricSvc`, disables the Recall feature | No (CoreAI end of life, service), Covered (Copilot app, Recall feature) | Marking a system package end of life is an undocumented servicing trick that can break updates. Disabling a system service without a documented reason is the kind of change PCOptimizer refuses elsewhere. The documented route for Recall is `AllowRecallEnablement` (row above) | Nothing new; Copilot and Microsoft 365 hub are already in the Debloat list |
| AppX: Cross Device Experience Host, Start Experiences App | Listed in WinUtil's removable AppX packages (`MicrosoftWindows.CrossDevice`, `Microsoft.StartExperiencesApp`) | Conditional | Neither is in PCOptimizer's list or its protected list. What breaks after removal (Phone Link features in Start, Start menu content) needs a check first | Add to `appx.json` after review, with the usual note when the Store does not offer them again |
| Notifications and calendar off | `HKCU\Software\Policies\Microsoft\Windows\Explorer\DisableNotificationCenter=1`, `PushNotifications\ToastEnabled=0` | Yes | Documented policy and Settings option. Gaming impact 0: Windows turns on Do Not Disturb automatically for fullscreen games | Opt-in tweak for Quiet and Office profiles; the explanation must say it also hides the calendar flyout |
| Windows.old cleanup | Not a separate item; part of `cleanmgr /VERYLOWDISK` | Yes | Microsoft documents removing previous Windows installations in Storage settings and Disk Cleanup | New cleanup category with a clear warning that going back to the previous version is no longer possible; default not selected |
| PowerShell telemetry opt-out | `POWERSHELL_TELEMETRY_OPTOUT=1` machine environment variable (inside the Telemetry tweak) | Yes, low value | Checked in `about_Telemetry`: the variable opts out when set to `true`, `yes` or `1` before PowerShell starts. Since PowerShell 7.6.2 on Windows, PowerShell also follows the Windows "Send optional diagnostic data" setting, which `privacy.telemetryOff` already turns off | Small tweak, shown only when PowerShell 7 older than 7.6.2 is installed |
| Windows Error Reporting service off | `wermgr` set to Disabled (inside the Telemetry tweak) | No | PCOptimizer already turns off error reporting through the documented policy (`privacy.errorReportingOff`). Disabling the process start type is not a documented control | none |
| Defender sample submission off | `Set-MpPreference -SubmitSamplesConsent 2` (inside the Telemetry tweak) | No | Lowers Defender's cloud protection; see 3.2 and section 4 | none |
| MPO "Fully Disabled" | `OverlayTestMode=5` plus `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\DisableOverlays=1` | No | `DisableOverlays` is not in the NVIDIA article that `gpu.mpoOff` cites, and no Microsoft page documents it | none |
| Microsoft Edge removal | Creates a dummy `MicrosoftEdge.exe` in `SystemApps\Microsoft.MicrosoftEdge_8wekyb3d8bbwe` to unlock `setup.exe --uninstall --system-level --force-uninstall --delete-profile` | No | Unsupported trick. Edge and WebView2 are serviced as part of Windows; Windows Update can reinstall it | Keep the current Edge text on the Debloat page. In the EEA, Windows offers an uninstall under Settings > Apps > Installed apps since the Digital Markets Act changes; link there once a Microsoft source for it is found |
| BitLocker off | `Disable-BitLocker -MountPoint $Env:SystemDrive` | No | Security loss, against the rule that the app never recommends turning off protection. Decryption takes hours | At most an informational finding that explains the measured cost of software encryption, without an apply button |
| RDP unsigned file warning off | `RedirectionWarningDialogVersion=1`, `RdpLaunchConsentAccepted=1` | No | Microsoft added the warning against phishing with RDP files. No performance gain | none |
| Old Start menu layout | `HKLM\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\3036241548\EnabledState=1` | No | Undocumented feature flag ID. WinUtil's own description says it does not work on newer builds | none |
| Store results in Start search off | `icacls store.db /deny *S-1-1-0:F` on the Store's local database | No | Denies access to an app's own data file; no documented setting exists. Store updates can fail or reset it | none |
| Services to Manual | `CscService` Disabled, `DiagTrack` Disabled, `MapsBroker` Manual, `StorSvc` Manual, `SharedAccess` Disabled, plus `SvcHostSplitThresholdInKB` set to the installed memory | No as a bundle | `SvcHostSplitThresholdInKB` is already rejected in `not-included.md`. `SharedAccess` Disabled breaks Mobile hotspot and Internet Connection Sharing. `CscService` Disabled breaks Offline Files. DiagTrack is already covered by `privacy.telemetryOff`; MapsBroker is in `services.json` | The Services page already allows any of these per service, with explanations. Add `CscService` and `StorSvc` to `services.json` if they need an explanation |
| Hide Home and Gallery in Explorer | `HKCU\Software\Classes\CLSID\{f874310e-...}` and `{e88865ea-...}\System.IsPinnedToNameSpaceTree=0`, open to This PC | No (hide), Covered (This PC) | The namespace values are not documented. `office.launchToThisPc` covers the documented part | none |
| Folder type discovery off | Deletes `Shell\Bags` and `Shell\BagMRU`, sets `Bags\AllFolders\Shell\FolderType=NotSpecified` | No | Undocumented Explorer storage. Deleting Bags loses every saved folder view, and WinUtil warns that grouping stops working | none |
| Hardware clock in UTC | `HKLM\SYSTEM\CurrentControlSet\Control\TimeZoneInformation\RealTimeIsUniversal=1` | No | Not documented by Microsoft for current Windows. Only useful for dual boot with Linux, which is out of scope | none |
| Razer software install block | `DriverSearching\SearchOrderConfig=0`, `Device Installer\DisableCoInstallers=1`, empties `C:\Windows\Installer\Razer` and denies write access to Everyone | No (folder lock), Conditional (`DisableCoInstallers`) | The folder trick is a hack. `SearchOrderConfig` overlaps with `updates.driversExcluded`. `DisableCoInstallers` blocks all vendor co-installers, not only Razer; it needs a Microsoft source before it can be considered, then Expert | none now |
| Logitech Download Assistant block | Stops the process, empties `Program Files\LogiDownloadAssistant` and denies write access | No | File system hack against one vendor | The Startup page can already turn off its autorun entry |
| Adobe URL block list | Adds Adobe activation and telemetry hosts to the hosts file | No | Blocks a vendor's licensing servers, which is close to license circumvention. Hosts file editing is out of scope | none |
| Disk Cleanup run | `cleanmgr.exe /d C: /VERYLOWDISK` and `DISM /StartComponentCleanup /ResetBase` | Covered, except `/ResetBase` (No) | `/ResetBase` makes installed updates impossible to uninstall; already in `not-included.md` | Windows.old as above |

### 2.2 Preference toggles (WinUtil "Customize Preferences")

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| Sticky Keys shortcut off | `HKCU\Control Panel\Accessibility\StickyKeys\Flags=506` | Yes | Documented accessibility setting. Relevant for gaming: pressing Shift five times opens a dialog that takes focus from the game | Tweak in `03-input-latency.json` or a new input group, also for the Filter Keys and Toggle Keys shortcuts, impact 0 with the focus loss explained, in the gaming profiles |
| Battery percentage in the system tray | `Explorer\Advanced\IsBatteryPercentageEnabled=1` | Yes | A Settings toggle (Power and battery) since Windows 11 22H2, shown only on devices with a battery. Only third-party pages describe the registry value so far; a Microsoft source is still needed | Laptop and Battery profiles, impact 0 |
| Long paths | `HKLM\SYSTEM\CurrentControlSet\Control\FileSystem\LongPathsEnabled=1` | Yes | Documented by Microsoft (Maximum file path limitation) | Office profile, impact 0 |
| Verbose sign-in status | `Policies\System\VerboseStatus=1` | Yes | Documented policy "Display highly detailed status messages" | Diagnostic tweak, no profile |
| Sign-in screen blur off | `Policies\Microsoft\Windows\System\DisableAcrylicBackgroundOnLogon=1` | Yes | Documented policy | Older PC profile next to `visual.transparencyOff` |
| Show hidden files | `Explorer\Advanced\Hidden=1` | Yes | Documented Folder Options setting | Office profile; low value |
| Num Lock at startup | `InitialKeyboardIndicators=2` for `.DEFAULT` and the current user | Conditional | Needs a current Microsoft source; the known article is old | Low value |
| Keyboard repeat delay | `HKCU\Control Panel\Keyboard\KeyboardDelay=0` (hidden inside the WinUtil visual effects tweak) | Conditional | Documented keyboard setting in Control Panel, but it is a preference, not a performance change, and WinUtil applies it without saying so | If added, a separate input tweak with an honest impact of 0 |
| Dark mode | `Personalize\AppsUseLightTheme=0`, `SystemUsesLightTheme=0` | Conditional | Documented Settings option, but personalization, not optimization | Only with a "Personalize" group; your decision |
| Taskbar alignment, search icon, Task View button, Chat | `TaskbarAl`, `SearchboxTaskbarMode`, `ShowTaskViewButton`, `TaskbarMn` | Conditional | Settings options; personalization | Same "Personalize" group |
| Scrollbars always visible | `Accessibility\DynamicScrollbars=0` | Conditional | Settings option; personalization | Same group |
| Window snapping | `Desktop\WindowArrangementActive` | Conditional | Settings option; personalization | Same group |
| Settings home page | `HKCU\...\Policies\Explorer\SettingsPageVisibility=show:home` or `hide:home` | Conditional | Documented policy; personalization | Same group |
| Start menu recommendations | `PolicyManager\current\device\Start\HideRecommendedSection`, `PolicyManager\current\device\Education\IsEducationEnvironment`, `Policies\Microsoft\Windows\Explorer\HideRecommendedSection` | Conditional | Checked: Policy CSP Start documents `HideRecommendedSection` for Pro, Enterprise and Education on Windows 11 22H2 and later. WinUtil also fakes an education environment in `PolicyManager\current`, which suggests the policy alone may not hide the section on Pro; that trick is undocumented (No) | The documented policy only, after a VM test on Pro shows that it works; otherwise the Start settings toggles |
| Lock screen off | `Policies\Microsoft\Windows\Personalization\NoLockScreen=1` | Conditional | The policy "Do not display the lock screen" is not in Microsoft's current Policy CSP reference, so its edition support could not be confirmed; community answers disagree | Needs a Microsoft source or a VM test on Home and Pro before it can be offered |
| Verbose blue screen | `CrashControl\DisplayParameters=1`, `DisableEmoticon=1` | Conditional | `DisableEmoticon` is not documented. `DisplayParameters` needs a source check | Diagnostic tweak with `DisplayParameters` only, if a source supports it |
| S0 sleep network connectivity | Power policy `f15576e8-...` `ACSettingIndex` | No | Already in `not-included.md` (Network off during Modern Standby) | none |
| S3 sleep instead of Modern Standby | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PlatformAoAcOverride=0` | No | Undocumented value. Many current boards have no S3 in firmware, so the result is unpredictable | none |
| New Outlook | Office `UseNewOutlook` values | No | Office app preference, out of scope | none |
| Game Mode, file extensions, mouse acceleration, MPO, Bing search | | Covered | | |

### 2.3 Features, fixes, DNS and panels

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| NFS client | Enables `ServicesForNFS-ClientOnly`, `ClientForNFS-Infrastructure`, `NFS-Administration`, then sets `AnonymousUID=0`, `AnonymousGID=0` and `fileaccess=755 SecFlavors=+sys -krb5 -krb5i` | Yes (feature), No (the extra configuration) | The features are documented (Pro and higher). Mapping anonymous access to UID and GID 0 and turning off Kerberos is a security choice the user should make, not the app | One entry in `features.json` that only enables the features |
| Network reset, second half | `netsh winsock reset` (covered) and `netsh int ip reset` | Yes | Documented netsh commands | Add `int ip reset` to the existing Winsock quick action, or link to Settings > Network > Network reset |
| chkdsk scan | `chkdsk /scan /perf` before SFC and DISM | Yes | Documented online scan; repairs only what can be fixed online | Health page next to SFC and DISM, with output streaming like SFC |
| More DNS presets | Cloudflare malware (1.1.1.2), Cloudflare malware and adult (1.1.1.3), OpenDNS, AdGuard ads and trackers, AdGuard ads, trackers, malware and adult | Yes | Public resolvers with published addresses | Add to `11-network-adapter.json`; the filtering ones are not offered by the benchmark, as in WinUtil |
| IPv6 DNS addresses | Sets `Primary6` and `Secondary6` for every provider | Yes | Same providers publish IPv6 addresses | Extend the existing presets |
| DNS over HTTPS | Registers each provider's DoH template with `Add-DnsClientDohServerAddress` and per interface `DohInterfaceSettings` | Yes | Windows 11 documents DoH (Settings, `netsh dns add encryption`, the DnsClient cmdlets) | Option on every preset that has a template; undo removes the registration |
| "Fastest" DNS | Benchmarks the eligible providers and applies the lowest latency one | Conditional | WinUtil times a TCP connect to port 53, which is not a DNS lookup. PCOptimizer's benchmark already measures real queries, so applying its winner is the better version | "Use the fastest" button on the Network page after an opt-in benchmark |
| Reset DNS to DHCP | "Default DHCP" choice | Covered | Undo restores the original servers | |
| Legacy F8 boot menu | `bcdedit /set bootmenupolicy legacy` and back to standard | Conditional | Boot configuration change, so Expert only under the rules | Better: a "Restart to advanced startup" button (`shutdown /r /o /t 0`), documented and without a boot change |
| NTP pool | `w32tm /config /manualpeerlist:"pool.ntp.org,0x8" /syncfromflags:MANUAL` | Conditional | Documented command; little benefit over `time.windows.com`. Changes which server the PC contacts, so it must be listed in `docs/PRIVACY.md` | Low priority |
| Old Control Panel shortcuts | Opens Computer Management, Control Panel, mouse, network connections, power, printers, programs, region, security, sound, system properties, date and time, firewall, System Restore | Yes | Launches inbox tools from System32 | Small section on the Tools page; use full System32 paths only |
| WinGet repair | `Install-PackageProvider NuGet`, `Install-Module Microsoft.WinGet.Client`, `Repair-WinGetPackageManager -AllUsers` | Conditional | Covered in part (Apps page links to App Installer in the Store). The repair loads a module from the PowerShell Gallery into an elevated session, which the elevation rule forbids | Keep the Store link |
| AutoLogon | Downloads `Autologon.exe` from live.sysinternals.com and starts it | No | Stores the password as an LSA secret, weakens sign-in security, and downloads and runs an exe elevated | none |
| PowerShell profile | Installs the CTT PowerShell 7 profile | No | Out of scope; runs third-party script code in every shell | none |
| OpenSSH server | Installs and starts the OpenSSH server capability | No | Out of scope; opens remote access to the PC | none |
| Registry backup, legacy media, .NET, Hyper-V, WSL, Sandbox, Update reset, SFC and DISM | | Covered | | |

### 2.4 Install tab

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| Uninstall apps | `winget uninstall --id <id> --source <source> --silent` | Yes | User-started, documented winget command | Uninstall button on the Apps page for installed catalog apps; reuse the trusted winget path and the ID allow list |
| Upgrade all apps | `winget upgrade --all --include-unknown --silent` | Conditional | User-started. `--all --include-unknown` also upgrades apps from other sources and apps outside the catalog, which the app has not reviewed | "Upgrade all catalog apps" using the existing `UpgradeArguments` per app, `--source winget` only |
| Large catalog | 236 apps in 10 categories (utilities, development, multimedia, pro tools, Microsoft tools, communications, documents, games, browsers, self-hosted) | Conditional | Every app needs a review (publisher, winget ID, license, whether it fits a gaming and profile app). Growing the list is fine; copying it is not | Add apps per profile need, each with a short reason |
| Free and open source badge | 146 of the 236 apps carry a `foss` flag shown as a badge, with a filter | Yes | Catalog metadata only | `foss` field in `apps.json` once the catalog grows |
| Chocolatey as package manager | Radio button WinGet or Chocolatey | No | Chocolatey community packages run install scripts elevated. Conflicts with the rule against running untrusted code elevated | none |
| Selection helpers | Search, collapse and expand categories, clear selection, selected count | Yes | UI only | Search box on the Apps page when the catalog grows |
| Microsoft Store apps (AppX install) | Installs Store apps by Store ID | Conditional | Uses winget with the msstore source, which needs the Store agreement and an account for some apps | Only if a catalog app is Store only |

### 2.5 Updates tab

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| Default settings | Removes the update policy values WinUtil sets (also `UX\Settings` deferral values and a legacy `hide:windowsupdate` Settings page block) and restores the update services | Covered in spirit | Undo on the Changes page restores the originals | |
| Security settings | Excludes drivers (covered), `DeferFeatureUpdates=1` with `DeferFeatureUpdatesPeriodInDays=365`, `DeferQualityUpdates=1` with 4 days, `AUOptions=3` (download and notify), `AUPowerManagement=0`, device metadata off (covered) | Conditional | Checked: the deferral policies in Policy CSP Update are listed for Pro, Enterprise and Education. Delaying quality updates delays security fixes, so the explanation must say so | Optional tweak "Delay feature updates" (Pro and higher), not recommended in any profile; quality delay only as Expert |
| Disable all updates | `NoAutoUpdate=1`, `AUOptions=1`, services disabled, update tasks disabled, SoftwareDistribution cleared | No | Already in `not-included.md` (Disabling Windows Update) | none |

### 2.6 Other WinUtil features

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| Export and import | Saves selected tweaks, features and apps as JSON and loads them again; can merge an import onto a preset | Yes | No system access beyond what the confirmation already shows | Export the applied changes (IDs and parameters) from the Changes page; import selects them and opens the normal confirmation. Same feature as O&O's .cfg files (3.1) |
| Automation | `-Config <file>` and `-Preset Standard\|Minimal\|Advanced` run every selected action without a window, with a time limit per step and a summary for the exit code | Conditional | A headless apply must not skip what the confirmation protects (Expert, Preview, boot-critical, anti-cheat). The current switches are read only by design | `--apply <export.json>` that refuses Expert and Preview items unless a second explicit switch is given, writes a report, and needs elevation like the UI |
| Windows 11 ISO builder and USB writer | Mounts an ISO, removes provisioned AppX packages, applies registry tweaks offline, removes scheduled tasks, injects exported drivers, writes `autounattend.xml`, sets the edition, writes a FAT32 USB stick | No | Very large feature, offline image servicing, can produce an unbootable installer, and parts of the unattend customization are not documented. Far outside "optimize this PC" | none; point to Microsoft's Media Creation Tool in the docs if needed |
| UI font scaling | Scales the WinUtil window fonts | Conditional | Windows text scaling (Settings > Accessibility > Text size) already applies to WPF | Only if users ask |
| Run O&O ShutUp10++ | Downloads and starts OOSU10 | No | Downloads and runs a third-party exe elevated | none |

---

## 3. Missing from O&O ShutUp10++

CLAUDE.md asks for your decision before big features modeled on O&O ShutUp10++ are added. The items marked with that note are such features.

### 3.1 App features

| Feature | What O&O does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| Edit mode | Tools > Edit Mode buffers every toggle, shows "n buffered", then Apply with one confirmation or Discard | Conditional (scope decision) | "Apply recommended" already batches its own list with one confirmation. Manual picks on the Tweaks page are applied one by one | A selection list on the Tweaks page that feeds the existing confirmation dialog |
| Undo history by session | Actions > Undo lists the last 10 sessions; undo the last one or roll back to any point | Yes | Backups already exist per change | Group the Changes page by apply session with "Undo this session" |
| Restore initial settings | Restores the state recorded at the first run of the app | Yes | The first backup of each setting already holds this | "Undo all" already does it for changed items; label it accordingly |
| Reset to Windows defaults | Sets every setting to its Windows default, also ones the app never changed | Conditional | Only possible where Microsoft documents the default; writing assumed defaults over a user's own settings is not honest | Only for tweaks whose explanation page names the documented default |
| Export and import .cfg; command line apply | `ID;+` or `ID;-` text file; a settings file can be applied from the command line | Yes | Same as WinUtil export and automation (2.6) | Same feature |
| Visible setting IDs and text search (Ctrl+F) | IDs shown in the list, search box | Yes | The Tweaks page has filters but no text search | Search box over title, summary and ID; IDs are already used by `--select` |
| AI status overview | "X of Y AI settings disabled", Copilot apps installed or not, Recall files found or not, overall status | Yes | Read only state that PCOptimizer can already compute | Card on the Overview or Debloat page |
| Recall data deletion | Scans `%LOCALAPPDATA%\CoreAIPlatform.00` and "securely erases" it (O&O SafeErase) | Conditional | Checked: setting `AllowRecallEnablement` to disabled removes Recall and deletes saved snapshots, and `DisableAIDataAnalysis` (already in PCOptimizer) also deletes them. Those documented routes are better than deleting files. "Secure erase" claims are not reliable on SSDs because of wear leveling, so the app must not make them | Offer `AllowRecallEnablement` (2.1); a cleanup category only if a case remains where the policies do not delete the data |
| Background service that re-applies settings (Premium) | A service checks settings and re-applies them after Windows updates or policy changes, without admin rights for the user | Conditional (scope decision) | The drift banner already finds reset changes. Automatic re-apply needs code running elevated at logon. Starting the single-file exe from a user-writable folder elevated breaks the elevation rule, so this needs an installed copy under Program Files and a signed exe | Scheduled task that runs an installed copy with `--reapply`, only for items the user marked; after code signing |
| Hybrid mode (Premium) | Stops re-applying settings that Group Policy keeps reverting; "Reset all blocks" | Conditional | Only meaningful together with the background service | Part of the service design, if it is built |
| Protection notifications (Premium) | Toasts when the service re-applied settings, with aggregation and flood limits | Conditional | Same dependency on the service | Same |
| Autostart and minimize to notification area (Premium) | Start at logon for one or all users; close to tray | Conditional | Same dependency; a gaming optimizer has no reason to run all the time otherwise | Same |
| Standard user mode | Runs without elevation for current user settings; "Restart as Admin" only for machine settings | Conditional | PCOptimizer runs elevated by design (the release manifest requires it). A non-elevated mode would need every page to handle missing rights | Not planned unless users ask |
| Current user and machine views | Separate tabs for HKCU and HKLM, dialog to align a user setting with the machine setting | Conditional | Low value for a single user gaming PC | Not planned |
| Named profiles (.pcfg, Premium) | Create, edit, export and import named profiles | Conditional | PCOptimizer profiles are built in. User-defined profiles are export and import with a name | Covered by export and import |
| More languages | English, German, Spanish, French, Italian, Japanese, Russian, Chinese | Conditional | A project decision: every tweak and check needs an explanation page per language with checked sources | Your decision |
| High contrast themes | App themes High Contrast Black and White | Yes | Accessibility; Windows high contrast mode should be respected by WPF-UI | Check the app under Windows contrast themes first; add support where it breaks |
| Windows Insider notice | Warns that Insider builds require diagnostic data and offers to turn off conflicting settings | Yes | Simple check of the Insider registration | Finding that explains the conflict with `privacy.telemetryOff` |
| Global configuration lock and green or blue toggle colors | Padlock for settings enforced by an enterprise configuration file; color scheme for switches | No | Enterprise feature; colors would break the Fluent vocabulary rule | none |

### 3.2 Settings by category

IDs are O&O's where the manual names them.

| O&O category and settings | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|
| **App privacy:** camera, microphone, notifications, radios, other devices and unpaired sync, documents, pictures, videos, file system, eye tracker, motion, voice activation (also when locked), generative AI and presence sensing (P082 to P085), passkeys, Bluetooth, input devices, stored passkey enumeration, custom sensors, serial ports, USB, Wi-Fi information, Wi-Fi Direct (P086 to P094 machine, P186 to P194 user) | Conditional (opt-in) | Documented `LetAppsAccess*` policies (Policy CSP Privacy). They break camera and microphone for Store apps; desktop apps such as Discord follow the separate "Let desktop apps access" setting, which must be explained honestly | New "App permissions" group, never in a gaming recommendation |
| **Telemetry:** OneSettings downloads, device name in diagnostic data (U008), crash dump collection limit (P096) | Yes | Checked in Policy CSP System: `DisableOneSettingsDownloads` and `LimitDumpCollection` (Pro and higher, Windows 11 21H2 and later), `AllowDeviceNameInDiagnosticData` (Pro and higher), all under `Software\Policies\Microsoft\Windows\DataCollection` | Add to `12-privacy-core.json` |
| **Windows privacy:** cloud consumer account state content (P095) | Conditional | Checked: `DisableConsumerAccountStateContent` is listed for Enterprise and Education only | Edition gated like `privacy.consumerFeaturesOff` |
| **Location:** sensors for location and orientation, Windows location provider, location scripting, remote connection location (P098) | Yes | Documented administrative templates (`DisableSensors`, `DisableWindowsLocationProvider`, `DisableLocationScripting`). Turning off sensors can stop screen rotation on tablets | Separate tweaks next to `privacy.locationOff`; the sensors one is not for convertibles |
| **Location:** location history | No | Checked: Microsoft deprecated and removed Location History in February 2025 | none |
| **Maps:** automatic map updates | No | Checked: the Maps app is deprecated and was removed from the Store in July 2025; the Maps platform APIs are deprecated | none |
| **Copilot and Windows AI:** Copilot, Copilot taskbar button, Copilot key, Recall enablement, AI data analysis (covered), Paint Image Creator, Cocreator, generative fill, Click to Do (C208), Settings agent (C209), AI in Notepad (C210), AI actions in File Explorer (C211) | Yes for Paint, Copilot key and Recall enablement; Conditional for the rest | Checked in Policy CSP WindowsAI: see 2.1. `TurnOffWindowsCopilot` (which also hides the taskbar button) is deprecated and does not apply to the new Copilot app; removing the app (already in Debloat) is the working route. Settings agent is Enterprise and Education only; Click to Do is listed for Insider builds only | Separate tweaks per feature, edition and build gated |
| **Search:** Microsoft account cloud search (M029), work or school cloud search (M030), device search history (M031), machine-wide Bing in Windows Search (M103) | Yes | Documented Search policies and Settings options; `privacy.cloudSearchOff` covers part | Extend the search tweaks |
| **Windows suggestions:** Start recommendations (M032), Start account notifications (M033), Settings account notifications (M034) | Conditional | `HideRecommendedSection` is documented for Pro and higher (checked) but needs the VM test from 2.2; the account notification items need a source | Group with the Start recommendations item from 2.2 |
| **Office and Microsoft 365:** connected experiences with content analytics, diagnostic data, surveys, CEIP, first-run movie (F020), Office sign-in (F021) | Conditional | Microsoft documents the Office privacy policies under `HKCU\Software\Policies\Microsoft\Office\16.0`. Shown only when Office is installed. Turning off connected experiences removes features such as Designer and translation | Office profile group, opt-in |
| **Edge (Chromium):** automatic sign-in from web to browser, visual search, form text prediction, cloud tab services, Microsoft Rewards, and more | Yes | Same Edge policy reference as 2.1 | Same "Edge privacy" group |
| **Windows Explorer:** sync provider ads (OneDrive notifications), OneDrive policy off, OneDrive network before sign-in, Start app suggestions (covered), recent items, office.com files in Explorer (P099) | Yes | Documented Explorer options and OneDrive policies (`DisableFileSyncNGSC`) | The OneDrive policy is a reversible alternative to the Debloat uninstall |
| **Lock screen:** fun facts and tips, notifications on the lock screen, lock screen camera | Yes | Documented Settings options and policies (`NoLockScreenCamera`) | Privacy group |
| **Lock screen:** Windows Spotlight | Conditional | Checked: `AllowWindowsSpotlight` and the related Spotlight policies are Enterprise and Education only; only `AllowThirdPartySuggestionsInWindowsSpotlight` is listed for Pro | Edition gated; the third-party suggestions one for Pro |
| **Windows Update:** optional and preview updates | Yes | Checked: `AllowOptionalContent` (value `SetAllowOptionalContent` under `Software\Policies\Microsoft\Windows\WindowsUpdate`) is listed for Pro and higher | Updates group in `15-updates-devices.json` |
| **Windows Update:** deferring of upgrades | Conditional | Checked: Pro and higher only (see 2.5) | Same as 2.5 |
| **Windows Update:** automatic Store app updates off | No | Store apps then miss security fixes | none |
| **Windows Update:** speech model updates off | Conditional | Low value: Windows speech recognition is deprecated (checked, December 2023). Needs a source | Low priority |
| **Security, privacy related:** Internet access of Windows Media DRM, password reveal button | Conditional (DRM), Yes (password reveal) | Documented policies (`DisableOnline` for WMDRM, `DisablePasswordReveal`). Legacy DRM services are deprecated (checked, September 2024), so the DRM item has little left to protect | Privacy group, opt-in |
| **Security, steps recorder** | Conditional | Checked: Steps Recorder is deprecated (November 2023) but not yet removed. The policy that turns it off needs a source | Low priority |
| **Security, wireless:** NFC, wireless displays, mobile broadband, Wi-Fi Direct (S119), Bluetooth pairing | Conditional (Expert or opt-in) | Breaks Miracast, Nearby Sharing, Wi-Fi Direct printing and, on some adapters, Mobile hotspot | Expert only, with these effects named |
| **Mobile devices:** suggestions for using mobile devices | Yes | Settings option | Privacy group |
| **Defender and SpyNet:** sample submission, SpyNet (MAPS) membership, malware infection reporting | No | Lowers cloud protection, which neither anti-cheats nor the security rules justify. Checked: with tamper protection on, "Cloud protection remains enabled" and "Attempts to modify Microsoft Defender Antivirus settings through the registry are blocked", so the change may not even stick | none |
| **Miscellaneous:** KMS online activation off | No | Affects license validation, no benefit | none |
| **Miscellaneous:** network connectivity status indicator (active probing) off | No | Breaks the "No internet" detection and captive portal sign-in in hotels and trains | none |
| **Activity history and clipboard, sync, Phone Link, feedback, tips, consumer features, Game Bar** | Covered | | |
| **Features Windows 11 no longer has:** Cortana (standalone app deprecated June 2023), Wi-Fi Sense (removed in Windows 10), Edge legacy (Do Not Track, form suggestions, search history, Edge bar; EdgeHTML no longer developed and not in Windows 11), People icon (deprecated in 1909, not in Windows 11), Timeline (retired in Windows 11) | No | The features no longer exist on Windows 11 24H2 or later, which is the app's minimum (deprecation dates checked on Microsoft's deprecated features page) | none |

---

## 4. Done by the competitors, refused by PCOptimizer on purpose

These are already explained with sources in `docs/not-included.md` and are not gaps:

- WinUtil Teredo off, IPv6 off, WPBT off, S0 network off, svchost split threshold, Disk Cleanup with `/ResetBase`, Windows Update off.
- WinUtil and O&O Defender sample submission off (O&O also SpyNet and malware reporting).
- WinUtil and O&O "Disable automatic Windows Updates".

Items added by this comparison that should go into `not-included.md` if you agree: Edge removal trick, BitLocker off, RDP warning off, Start menu feature flag, Store database lock, folder type discovery, UTC hardware clock, Razer and Logitech folder locks, Adobe hosts list, S3 sleep override, MPO `DisableOverlays`, Windows AI end of life marking, AutoLogon, Chocolatey, KMS online activation, NCSI probing, Store app auto updates off, Defender SpyNet and malware reporting.

## 5. Recommended order

1. **Quick, documented, fits the profiles:** Paint AI policies, Copilot key, `AllowRecallEnablement`, Sticky Keys and Filter Keys shortcuts, Reserved Storage, sign-in blur, long paths, NFS feature, `int ip reset`, `chkdsk /scan`, "Restart to advanced startup", telemetry extras (OneSettings, device name, dump limit).
2. **Biggest usability gaps:** export and import, text search on the Tweaks page, Uninstall and "Upgrade all catalog apps", undo grouped by session.
3. **Network:** more DNS presets, IPv6 addresses, DNS over HTTPS, "use the fastest" from the existing benchmark.
4. **Privacy breadth against O&O (needs your scope decision):** app permissions group, Edge privacy group, Office group, location extras, lock screen items, OneDrive policy, optional updates policy.
5. **Cleanup:** Windows.old.
6. **Needs a VM test first:** Start recommendations policy on Pro, lock screen policy, battery percentage value.
7. **Later, after code signing and an installer:** headless `--apply`, automatic re-apply after Windows updates.

Every new tweak still needs its EN and DE explanation page with the required sections, a summary of at most 200 characters and opened sources before it ships. New network use (NTP pool, DoH resolvers) goes into `docs/PRIVACY.md` in both languages.

## 6. Decisions needed from you

- Whether the O&O style privacy expansion (3.2, groups marked opt-in) is in scope, given the CLAUDE.md rule on features modeled on O&O ShutUp10++.
- Whether PCOptimizer should get a "Personalize" group for Settings-only toggles (dark mode, taskbar, scrollbars, Snap) or stay strictly on optimization and privacy.
- Whether an installer and background re-apply (with its notifications, autostart and hybrid mode) are wanted at all, or the drift banner stays the only re-apply path.
- Whether more languages than English and German are planned.

## 7. Review log

A second pass on 2026-10-10 compared the document against every entry in WinUtil's configuration files, the action scripts and the O&O manual again, and opened the Microsoft pages behind the edition claims. Changes against the first version:

- **Corrected:** DNS over HTTPS was described as missing in WinUtil; WinUtil sets DoH templates for every provider. Camera and Media Player were listed as missing from PCOptimizer's Debloat list; both are in it. The lock screen policy was described as documented for Enterprise and Education only; that could not be confirmed. The Start recommendations policy is documented for Pro. Steps Recorder is deprecated, not removed.
- **Verdicts changed by the Microsoft pages:** Settings agent and the Copilot app removal policy are Enterprise and Education only; Click to Do is listed for Insider builds only; Spotlight is Enterprise and Education only; Recall data deletion should use `AllowRecallEnablement` instead of deleting files; Maps auto update and Location History are moot because Microsoft removed those features; PowerShell telemetry opt-out matters only before 7.6.2.
- **Added:** WinUtil MPO "Fully Disabled", the two extra removable AppX packages, IPv6 DNS, "Fastest" DNS, the NFS security configuration, the FOSS badge, the details of the headless run; O&O AI status overview, hybrid mode, notifications, autostart and tray, standard user mode, more languages, high contrast themes, cloud consumer account state content (P095).
- **Confirmed as written:** the WinGet repair loads a PowerShell Gallery module; WinUtil's system repair runs chkdsk, SFC and DISM; the update deferral policies are Pro and higher; tamper protection keeps cloud protection on and blocks registry changes to Defender settings.

## Sources

Competitors:

- WinUtil repository, configuration: <https://github.com/ChrisTitusTech/winutil/tree/main/config>
- WinUtil repository, functions: <https://github.com/ChrisTitusTech/winutil/tree/main/functions>
- WinUtil README (presets, automation): <https://github.com/ChrisTitusTech/winutil/blob/main/README.md>
- O&O ShutUp10++ product page (version 3.6.1135): <https://www.oo-software.com/en/shutup10>
- O&O manual, introduction: <https://manuals.oo-software.com/ooshutup10/en/docs/intro>
- O&O manual, privacy settings overview: <https://manuals.oo-software.com/ooshutup10/en/docs/features/overview>
- O&O manual, feature comparison: <https://manuals.oo-software.com/ooshutup10/en/docs/introduction/feature-comparison>
- O&O manual, AI removal: <https://manuals.oo-software.com/ooshutup10/en/docs/common-features/ai-removal>
- O&O manual, PDF (edit mode, undo history, profiles, settings dialog, command line, FAQ): <https://manuals.oo-software.com/ooshutup10/oosu10-manual-en.pdf>

Microsoft pages opened in the review:

- Policy CSP WindowsAI: <https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai>
- Policy CSP Experience: <https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience>
- Policy CSP Start: <https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-start>
- Policy CSP System: <https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system>
- Policy CSP Update: <https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update>
- Policy CSP ADMX_ControlPanelDisplay (no lock screen policy listed): <https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-controlpaneldisplay>
- Tamper protection overview: <https://learn.microsoft.com/en-us/defender-endpoint/prevent-changes-to-security-settings-with-tamper-protection>
- Deprecated features in the Windows client: <https://learn.microsoft.com/en-us/windows/whats-new/deprecated-features>
- PowerShell about_Telemetry: <https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_telemetry>
