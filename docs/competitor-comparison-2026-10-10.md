# Feature comparison with WinUtil and O&O ShutUp10++, 2026-10-10

Scope: every feature of Chris Titus Tech's Windows Utility (WinUtil) and O&O ShutUp10++ that PCOptimizer 0.4.0 (commit 20650b3) does not have, with a verdict on whether it can be built under the rules in CLAUDE.md, and why not where it cannot. Read only; no code was changed.

## Method and limits

- **PCOptimizer:** read from the catalog (`src/Optimizer.Core/Catalog/Tweaks/*.json`, `Catalog/Data/*.json`), the engine folders (Cleanup, Tools, Apps, Network, Debloat, Backup), the views and `Strings.resx`, the explanation pages, README, CONTRIBUTING and `docs/not-included.md`.
- **WinUtil:** the `main` branch on 2026-10-10. Parsed all 67 entries of `config/tweaks.json`, all 33 of `feature.json`, and `applications.json` (236 apps), `appx.json`, `appnavigation.json`, `dns.json` and `preset.json`. Read the scripts in `functions/public` and `functions/private` that implement actions (DNS, update policies, system repair, ISO builder, import and export, headless run, autologon, network reset, WinGet repair). Registry values and commands quoted below come from those files.
- **O&O ShutUp10++:** version 3.6.1135 (release date 2026-09-29 on the product page). O&O does not publish the full list of its nearly 300 settings. This document uses the manual (web and PDF, 116 pages): the 22 categories with their example settings, the table of controls added in 3.2 to 3.4 (with IDs), the feature comparison, AI removal, edit mode, undo history, profiles, the settings dialog and the FAQ. Settings that exist in the app but are not named in the manual are not listed one by one; they fall under their category row.
- **Sources:** every claim that a verdict depends on was checked against a primary source: the Microsoft Policy CSP reference (about 50 pages, read in full), Microsoft command, API and support pages, the Edge policy reference, Brave's policy definitions in brave-core, NVIDIA's MPO registry file, and the DNS providers' own pages. Section 8 lists each claim, its source and the result. Items without a primary source are marked as such and are "No" or "Conditional" until one is found.
- **Editions:** Policy CSP pages list Pro, Enterprise and Education, never Home. PCOptimizer already hides Group Policy tweaks on Home; every policy based item below inherits that. "Pro and higher" below means the CSP page lists Pro.

## Verdicts

| Verdict | Meaning |
|---|---|
| **Yes** | Fits the rules: documented setting, reversible, honest impact. Can go into the catalog or a page. |
| **Conditional** | Buildable with a restriction: Expert only, edition or build gated, opt-in only, only when an app is installed, needs a VM test first, or needs your decision under the scope rule. |
| **No** | Should not be built. The reason column says why: undocumented, security loss, hack, removed or deprecated in Windows 11, management only (MDM) policy, against a CLAUDE.md rule, or out of scope. |
| **Covered** | PCOptimizer already has it (section 1). |

## Summary

- WinUtil: 70 features missing in PCOptimizer (section 2): 23 Yes, 21 Conditional, 26 No. Items already rejected in `not-included.md` and covered items are not counted.
- O&O ShutUp10++: 19 app features (7 Yes, 11 Conditional, 1 No) and 27 setting groups (10 Yes, 10 Conditional, 7 No) missing, plus one group of settings for features that Windows 11 no longer has (section 3).
- Most of the O&O gap is privacy breadth (app permissions, Office, Edge, lock screen, AI policies). Most of the WinUtil gap is convenience (app catalog size, uninstall and upgrade all, export and import, DNS over HTTPS, Windows Update presets) and personalization toggles.
- Several policies that both competitors write only work on Enterprise and Education (Spotlight, Windows tips, consumer features, web results in Search, Settings agent, some Recall controls), and several of O&O's wireless switches have no Group Policy or registry form at all. PCOptimizer should keep gating by edition instead of writing these everywhere.
- Things both tools do that PCOptimizer refuses on purpose are listed in section 4 and are already explained in `docs/not-included.md`.

---

## 1. Already covered by PCOptimizer

| Competitor feature | PCOptimizer equivalent | Difference |
|---|---|---|
| WinUtil Activity History off; O&O activity history | `privacy.activityHistoryOff` | none |
| WinUtil Hibernation off | `power.hibernateOff` | WinUtil also hides the Hibernate menu entry (`FlyoutMenuSettings\ShowHibernateOption=0`) |
| WinUtil Widgets remove; O&O widgets, news and interests | `background.widgetsOff` (policy `Dsh\AllowNewsAndInterests`) | WinUtil uninstalls `Microsoft.WidgetsPlatformRuntime` and `MicrosoftWindows.Client.WebExperience`; PCOptimizer uses the reversible policy |
| WinUtil Location tracking off; O&O app location access | `privacy.locationOff` | WinUtil also sets `Sensor\Overrides\{BFA794E4-...}\SensorPermissionState=0`, `HKLM\SYSTEM\Maps\AutoUpdateEnabled=0` and disables `lfsvc` (see 3.2) |
| WinUtil ConsumerFeatures off; O&O Microsoft consumer features (P097) | `privacy.consumerFeaturesOff` | PCOptimizer offers it only on Enterprise and Education, because Microsoft lists `AllowWindowsConsumerFeatures` for those editions only. WinUtil writes it on every edition, where it may do nothing |
| WinUtil Telemetry off; O&O telemetry category | `privacy.telemetryOff`, `privacy.advertisingIdOff`, `privacy.tailoredExperiencesOff`, `privacy.inkingTypingOff`, `privacy.onlineSpeechOff`, `privacy.appLaunchTrackingOff`, `privacy.feedbackNotificationsOff`, `privacy.errorReportingOff` | WinUtil writes `AllowTelemetry=0` under `CurrentVersion\Policies\DataCollection`; the value 0 is only honored on Enterprise and Education, which PCOptimizer's page already says. WinUtil also disables `wermgr`, sets Defender `SubmitSamplesConsent=2` and sets `POWERSHELL_TELEMETRY_OPTOUT=1` (see 2.1) |
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
| WinUtil Device companion apps off | `privacy.deviceMetadataOff` | Microsoft deprecated device metadata in May 2025; the policy still applies while the feature exists |
| WinUtil Updates: driver exclusion; O&O W010 | `updates.driversExcluded` | none |
| WinUtil Bing search toggle; O&O web search in Start, search highlights, cloud search | `privacy.webSearchOff`, `privacy.cloudSearchOff`, `privacy.searchHighlightsOff` | none |
| WinUtil Edge (part) | `background.edgeBoostOff` | the other Edge policies are missing (see 2.1) |
| WinUtil Environment report | `--report`, Settings: report a problem, copy log | none |
| WinUtil "Select installed tweaks", live toggle state | Tweaks page shows the applied state of every tweak | none |
| WinUtil Undo all | Changes page: Undo all | none |
| WinUtil Show installed apps | Apps page shows installed state | none |
| WinUtil Presets (Standard, Minimal, Advanced) | Profiles and "Apply recommended" | PCOptimizer gives a reason per item |
| O&O clipboard history, cloud clipboard | `privacy.clipboardHistoryOff`, `privacy.cloudClipboardOff` | none |
| O&O sync of all settings | `privacy.settingsSyncOff` | O&O can also turn off only credentials, language or design; PCOptimizer turns off all |
| O&O Phone Link, PC to mobile | `privacy.phoneLinkOff` (`AllowPhonePCLinking`, `EnableMmx`), `privacy.crossDeviceOff` | none |
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
| Edge debloat | 17 Edge policies under `HKLM\SOFTWARE\Policies\Microsoft\Edge` and `EdgeUpdate`: `PersonalizationReportingEnabled=0`, `ShowRecommendationsEnabled=0`, `HideFirstRunExperience=1`, `UserFeedbackAllowed=0`, `ConfigureDoNotTrack=1`, `AlternateErrorPagesEnabled=0`, `EdgeCollectionsEnabled=0`, `EdgeShoppingAssistantEnabled=0`, `MicrosoftEdgeInsiderPromotionEnabled=0`, `ShowMicrosoftRewards=0`, `WebWidgetAllowed=0`, `DiagnosticData=0`, `EdgeAssetDeliveryServiceEnabled=0`, `WalletDonationEnabled=0`, `DefaultBrowserSettingsCampaignEnabled=0`, `CreateDesktopShortcutDefault=0`, and an extension block list entry | Yes | Checked in the Edge policy reference: every policy exists and none is limited to domain joined or MDM managed devices. `EdgeCollectionsEnabled` is obsolete and does nothing after Edge 152; `WalletDonationEnabled` and `WebWidgetAllowed` are deprecated. `CreateDesktopShortcutDefault` is an Edge Update policy | One catalog group "Edge privacy" next to `background.edgeBoostOff`, impact 0, shown only when Edge is installed. Split into separate tweaks so a user can keep Rewards or the shopping assistant. Leave out the obsolete and deprecated three and the extension block list (it blocks one extension ID without a stated reason) |
| Brave debloat | 12 policies under `HKLM\SOFTWARE\Policies\BraveSoftware\Brave`: Rewards, Wallet, VPN, Leo AI, stats ping, News, Talk, Tor, P3A, URL keyed data, Safe Browsing extended reporting, metrics | Yes, opt-in | Checked: all 9 Brave specific policies (`BraveRewardsDisabled`, `BraveWalletDisabled`, `BraveVPNDisabled`, `BraveAIChatEnabled`, `BraveStatsPingEnabled`, `BraveNewsDisabled`, `BraveTalkDisabled`, `TorDisabled`, `BraveP3AEnabled`) are defined in brave-core's policy templates; the other 3 are standard Chromium policies. Brave's support page could not be downloaded (403) and should be cited from a browser. No gaming impact | Opt-in group, shown only when Brave is installed, not in any profile recommendation |
| Reserved Storage off | `DISM /Online /Set-ReservedStorageState /State:Disabled` | Yes | Checked: Microsoft's DISM reference documents the option (online images only); it can fail while reserved storage is in use by servicing. Reversible with `/State:Enabled` | Tweak for the Older PC profile on drives under a size threshold, with a warning that feature updates may need the space and a retry note. Process runner action with undo |
| Windows AI: policies | `SettingsPageVisibility=hide:aicomponents`, `HKLM\SOFTWARE\Policies\WindowsNotepad\DisableAIFeatures=1` | Yes for Paint, Notepad, Recall enablement; Conditional for Copilot key and Click to Do; No for the Settings page | Checked in Policy CSP WindowsAI: `DisableImageCreator`, `DisableCocreator`, `DisableGenerativeFill` (Pro and higher), `AllowRecallEnablement` (Pro and higher, 24H2 with KB5055627; disabling it removes the Recall bits and deletes saved snapshots), `SetCopilotHardwareKey` (Pro and higher; it only chooses which app the key opens, it cannot turn the key off), `DisableClickToDo` (Pro and higher, listed for Insider builds only). `DisableSettingsAgent` and `RemoveMicrosoftCopilotApp` are Enterprise and Education only. Notepad: Microsoft's "Manage Notepad" page documents exactly `DisableAIFeatures=1` under `HKLM\SOFTWARE\Policies\WindowsNotepad` (Windows 11 22H2, Notepad 11.2503.16.0 or later). `SettingsPageVisibility` is a documented policy, but `aicomponents` is not in Microsoft's list of Settings pages | Separate tweaks per AI feature next to `background.aiOff`, edition and build gated (see also 3.2 Copilot and Windows AI) |
| Windows AI: removal | Marks `MicrosoftWindows.Client.CoreAI` end of life in `AppxAllUserStore\EndOfLife`, removes all `*Copilot*` packages and the Microsoft 365 hub, runs `winget uninstall Copilot`, disables `WSAIFabricSvc`, disables the Recall feature | No (CoreAI end of life, service), Covered (Copilot app, Recall feature) | Marking a system package end of life is an undocumented servicing trick that can break updates. Disabling a system service without a documented reason is the kind of change PCOptimizer refuses elsewhere. The documented route for Recall is `AllowRecallEnablement` (row above) | Nothing new; Copilot and Microsoft 365 hub are already in the Debloat list |
| AppX: Cross Device Experience Host | Listed in WinUtil's removable AppX packages (`MicrosoftWindows.CrossDevice`) | No | It is the Windows component behind phone and cross-device features (mobile device in File Explorer and similar); only community guides describe removing it. PCOptimizer already turns those features off through documented policies (`privacy.phoneLinkOff`, `privacy.crossDeviceOff`) | none |
| AppX: Start Experiences App | Listed in WinUtil's removable AppX packages (`Microsoft.StartExperiencesApp`) | Conditional | Only a community answer describes it (as the feed provider that pushes content to Start, search and the taskbar); no Microsoft page found. What breaks after removal is unknown | VM test before adding it to `appx.json` |
| Notifications and calendar off | `HKCU\Software\Policies\Microsoft\Windows\Explorer\DisableNotificationCenter=1`, `PushNotifications\ToastEnabled=0` | Yes | Checked: `DisableNotificationCenter` is the documented policy "Remove Notifications and Action Center" (Pro and higher), and "Turn off toast notifications" (`NoToastApplicationNotification`) is documented too. `ToastEnabled` is only the value behind the Settings switch. Gaming impact 0: Windows turns on Do Not Disturb automatically for fullscreen games | Opt-in tweak for Quiet and Office profiles using the two policies; the explanation must say it also hides the calendar flyout |
| Windows.old cleanup | Not a separate item; part of `cleanmgr /VERYLOWDISK` | Yes | Checked: Microsoft's support article "Delete your previous version of Windows" describes removing it in Storage settings; going back is no longer possible afterwards, and Windows deletes it on its own after 10 days | New cleanup category with a clear warning; default not selected |
| PowerShell telemetry opt-out | `POWERSHELL_TELEMETRY_OPTOUT=1` machine environment variable (inside the Telemetry tweak) | Yes, low value | Checked in `about_Telemetry`: the variable opts out when set to `true`, `yes` or `1` before PowerShell starts. Since PowerShell 7.6.2 on Windows, PowerShell also follows the Windows "Send optional diagnostic data" setting, which `privacy.telemetryOff` already turns off | Small tweak, shown only when PowerShell 7 older than 7.6.2 is installed |
| Windows Error Reporting service off | `wermgr` set to Disabled (inside the Telemetry tweak) | No | PCOptimizer already turns off error reporting through the documented policy (`privacy.errorReportingOff`). Disabling the process start type is not a documented control | none |
| Defender sample submission off | `Set-MpPreference -SubmitSamplesConsent 2` (inside the Telemetry tweak) | No | Lowers Defender's cloud protection; see 3.2 and section 4 | none |
| MPO "Fully Disabled" | `OverlayTestMode=5` plus `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\DisableOverlays=1` | No | Checked: NVIDIA's `mpo_disable.reg` (attached to article 5157) sets only `OverlayTestMode=5`, and `mpo_restore.reg` only removes it. No Microsoft or NVIDIA page documents `DisableOverlays` | none |
| Microsoft Edge removal | Creates a dummy `MicrosoftEdge.exe` in `SystemApps\Microsoft.MicrosoftEdge_8wekyb3d8bbwe` so that `setup.exe --uninstall --system-level --force-uninstall --delete-profile` | No | Unsupported trick. Edge and WebView2 are serviced as part of Windows; Windows Update can reinstall it | Keep the current Edge text on the Debloat page. Microsoft's EU policy blog (March 2024) confirms that users in the EEA can uninstall Edge with the standard Windows mechanisms; Windows decides this by the region chosen at setup. Link to Settings > Apps > Installed apps for those users |
| BitLocker off | `Disable-BitLocker -MountPoint $Env:SystemDrive` | No | Security loss, against the rule that the app never recommends turning off protection. Decryption takes hours | At most an informational finding that explains the measured cost of software encryption, without an apply button |
| RDP unsigned file warning off | `RedirectionWarningDialogVersion=1`, `RdpLaunchConsentAccepted=1` | No | Checked: Microsoft added these warnings with the April 2026 security update against phishing with RDP files, and documents `RedirectionWarningDialogVersion` only as a temporary way back. No performance gain | none |
| Old Start menu layout | `HKLM\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\3036241548\EnabledState=1` | No | Undocumented feature flag ID. WinUtil's own description says it does not work on newer builds | none |
| Store results in Start search off | `icacls store.db /deny *S-1-1-0:F` on the Store's local database | No | Denies access to an app's own data file; no documented setting exists. Store updates can fail or reset it | none |
| Services to Manual | `CscService` Disabled, `DiagTrack` Disabled, `MapsBroker` Manual, `StorSvc` Manual, `SharedAccess` Disabled, plus `SvcHostSplitThresholdInKB` set to the installed memory | No as a bundle | `SvcHostSplitThresholdInKB` is already rejected in `not-included.md`. `SharedAccess` Disabled breaks Mobile hotspot and Internet Connection Sharing. `CscService` Disabled breaks Offline Files. DiagTrack is already covered by `privacy.telemetryOff`; MapsBroker is in `services.json` | The Services page already allows any of these per service, with explanations. Add `CscService` and `StorSvc` to `services.json` if they need an explanation |
| Hide Home and Gallery in Explorer | `HKCU\Software\Classes\CLSID\{f874310e-...}` and `{e88865ea-...}\System.IsPinnedToNameSpaceTree=0`, open to This PC | No (hide), Covered (This PC) | The namespace values are not documented. `office.launchToThisPc` covers the documented part | none |
| Folder type discovery off | Deletes `Shell\Bags` and `Shell\BagMRU`, sets `Bags\AllFolders\Shell\FolderType=NotSpecified` | No | Undocumented Explorer storage. Deleting Bags loses every saved folder view, and WinUtil warns that grouping stops working | none |
| Hardware clock in UTC | `HKLM\SYSTEM\CurrentControlSet\Control\TimeZoneInformation\RealTimeIsUniversal=1` | No | Checked: no Microsoft documentation exists; a Microsoft kernel team member is quoted saying the flag is not covered by documentation or regression tests and is not recommended. Only useful for dual boot with Linux, which is out of scope | none |
| Razer software install block | `DriverSearching\SearchOrderConfig=0`, `Device Installer\DisableCoInstallers=1`, empties `C:\Windows\Installer\Razer` and denies write access to Everyone | No | The folder trick is a hack. Checked: no Microsoft documentation exists for `DisableCoInstallers`. The documented driver search policies ("Specify search order for device driver source locations", "Turn off Windows Update device driver searching") overlap with `updates.driversExcluded` | none |
| Logitech Download Assistant block | Stops the process, empties `Program Files\LogiDownloadAssistant` and denies write access | No | File system hack against one vendor | The Startup page can already turn off its autorun entry |
| Adobe URL block list | Adds Adobe activation and telemetry hosts to the hosts file | No | Blocks a vendor's licensing servers, which is close to license circumvention. Hosts file editing is out of scope | none |
| Disk Cleanup run | `cleanmgr.exe /d C: /VERYLOWDISK` and `DISM /StartComponentCleanup /ResetBase` | Covered, except `/ResetBase` (No) | `/ResetBase` makes installed updates impossible to uninstall; already in `not-included.md` | Windows.old as above |

### 2.2 Preference toggles (WinUtil "Customize Preferences")

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| Sticky Keys shortcut off | `HKCU\Control Panel\Accessibility\StickyKeys\Flags=506` | Yes | Checked: the `STICKYKEYS` structure documents `SKF_HOTKEYACTIVE` ("the user can turn the StickyKeys feature on and off by pressing the SHIFT key five times"); `SystemParametersInfo` documents `SPI_SETSTICKYKEYS`, `SPI_SETFILTERKEYS` and `SPI_SETTOGGLEKEYS`. 506 is the default flags without that bit. Relevant for gaming: the dialog takes focus from the game | Use `SystemParametersInfo` with `SPIF_UPDATEINIFILE` to clear only the hotkey flag (not a raw registry write), for Sticky, Filter and Toggle Keys; impact 0 with the focus loss explained; gaming profiles |
| Battery percentage in the system tray | `Explorer\Advanced\IsBatteryPercentageEnabled=1` | Conditional | Checked: the Settings toggle (Power and battery > Battery percentage) arrived with the new battery icon in Windows Insider builds in early 2025 (Dev build 26120.3000); its Release Preview rollout (26100.3321) was paused and resumed later, so availability depends on the build. Microsoft does not document the registry value | On builds that have the toggle, link to the Settings page from the Laptop and Battery profiles instead of writing the value |
| Long paths | `HKLM\SYSTEM\CurrentControlSet\Control\FileSystem\LongPathsEnabled=1` | Yes | Checked: documented policy "Enable Win32 long paths" (Pro and higher) and the "Maximum path length limitation" page. It only helps apps whose manifest declares `longPathAware` | Office profile, impact 0, with that limit explained |
| Verbose sign-in status | `Policies\System\VerboseStatus=1` | Yes | Checked: documented policy "Display highly detailed status messages" (Pro and higher) | Diagnostic tweak, no profile |
| Sign-in screen blur off | `Policies\Microsoft\Windows\System\DisableAcrylicBackgroundOnLogon=1` | Yes | Checked: documented policy "Show clear logon background" (Pro and higher) | Older PC profile next to `visual.transparencyOff` |
| Lock screen off | `Policies\Microsoft\Windows\Personalization\NoLockScreen=1` | Yes | Checked: documented policy "Do not display the lock screen" (Pro and higher, Windows 11 21H2 and later). Users who do not have to press Ctrl+Alt+Del see their tile instead of the lock screen | Office or Quiet profile, opt-in |
| Show hidden files | `Explorer\Advanced\Hidden=1` | Yes | Folder Options setting described in Microsoft's support articles | Office profile; low value |
| Num Lock at startup | `InitialKeyboardIndicators=2` for `.DEFAULT` and the current user | Conditional | Checked: Microsoft's archived Windows 2000 registry reference documents the HKCU value (0 off, 2 on) and says Windows saves the state at sign-out. The `.DEFAULT` hive and current behavior come only from community answers, and Fast Startup can override it | Low value; VM test first |
| Keyboard repeat delay | `HKCU\Control Panel\Keyboard\KeyboardDelay=0` (hidden inside the WinUtil visual effects tweak) | Conditional | Checked: `SPI_SETKEYBOARDDELAY` is documented. It is a preference, not a performance change, and WinUtil applies it without saying so | If added, a separate input tweak through `SystemParametersInfo`, impact 0 |
| Dark mode | `Personalize\AppsUseLightTheme=0`, `SystemUsesLightTheme=0` | Conditional | Settings option, but personalization, not optimization | Only with a "Personalize" group; your decision |
| Taskbar alignment, search icon, Task View button, Chat | `TaskbarAl`, `SearchboxTaskbarMode`, `ShowTaskViewButton`, `TaskbarMn` | Conditional | Settings options; personalization. The registry values are not documented | Same "Personalize" group |
| Scrollbars always visible | `Accessibility\DynamicScrollbars=0` | Conditional | Settings option; personalization | Same group |
| Window snapping | `Desktop\WindowArrangementActive` | Conditional | Settings option; personalization. `SPI_SETWINARRANGING` is the documented API | Same group |
| Settings home page | `HKCU\...\Policies\Explorer\SettingsPageVisibility=show:home` or `hide:home` | Conditional | Checked: the policy "Settings Page Visibility" is documented (Pro and higher); personalization | Same group |
| Start menu recommendations | `PolicyManager\current\device\Start\HideRecommendedSection`, `PolicyManager\current\device\Education\IsEducationEnvironment`, `Policies\Microsoft\Windows\Explorer\HideRecommendedSection` | Conditional | Checked: `HideRecommendedSection` is documented for Pro and higher on Windows 11 22H2 and later. WinUtil also fakes an education environment in `PolicyManager\current`, which suggests the policy alone may not hide the section on Pro; that trick is undocumented (No). `HideRecentlyAddedApps` and `HideRecommendedPersonalizedSites` are documented for Pro as well | The documented policies only, after a VM test on Pro shows that they work |
| Verbose blue screen | `CrashControl\DisplayParameters=1`, `DisableEmoticon=1` | No | Checked: no Microsoft page documents either value; only WinUtil and third-party guides describe them | none |
| S0 sleep network connectivity | Power policy `f15576e8-...` `ACSettingIndex` | No | Already in `not-included.md` (Network off during Modern Standby) | none |
| S3 sleep instead of Modern Standby | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PlatformAoAcOverride=0` | No | Checked: no Microsoft documentation, only community guides and forum answers. Many current boards have no S3 in firmware, so the result is unpredictable | none |
| New Outlook | Office `UseNewOutlook` values | No | Office app preference, out of scope | none |
| Game Mode, file extensions, mouse acceleration, MPO, Bing search | | Covered | | |

### 2.3 Features, fixes, DNS and panels

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| NFS client | Enables `ServicesForNFS-ClientOnly`, `ClientForNFS-Infrastructure`, `NFS-Administration`, then sets `AnonymousUID=0`, `AnonymousGID=0` and `fileaccess=755 SecFlavors=+sys -krb5 -krb5i` | Yes (feature), No (the extra configuration) | Checked: Microsoft's NFS overview lists Client for NFS (NFSv2 and NFSv3) for all supported Windows client versions. Mapping anonymous access to UID and GID 0 and turning off Kerberos is a security choice the user should make, not the app | One entry in `features.json` that only enables the features |
| Network reset, second half | `netsh winsock reset` (covered) and `netsh int ip reset` | Conditional | Checked: Microsoft's article "Reset TCP/IP by using the NetShell utility" documents it; it rewrites `Tcpip\Parameters` and `DHCP\Parameters`, so static IP and DNS settings are lost, including PCOptimizer's DNS presets, and their Undo can then no longer restore the original state | Add it with a warning, and mark DNS and adapter changes on the Changes page as reset afterwards; or link to Settings > Network > Network reset |
| chkdsk scan | `chkdsk /scan /perf` before SFC and DISM | Yes | Checked: `/scan` runs an online scan on NTFS; `/perf` uses more system resources and can slow other tasks | Health page next to SFC and DISM, with `/scan` only, output streaming like SFC |
| More DNS presets | Cloudflare malware (1.1.1.2), Cloudflare malware and adult (1.1.1.3), OpenDNS, AdGuard default, AdGuard family | Yes | Checked: Cloudflare's setup page lists 1.1.1.2 and 1.1.1.3 with IPv6 and DoH URLs; AdGuard's provider list gives AdGuard default (ads, trackers, phishing) and family (plus adult sites and safe search), and OpenDNS. WinUtil's addresses match. OpenDNS's own page was not opened | Add to `11-network-adapter.json`; the filtering ones are not offered by the benchmark, as in WinUtil |
| IPv6 DNS addresses | Sets `Primary6` and `Secondary6` for every provider | Yes | Same provider pages | Extend the existing presets |
| DNS over HTTPS | Registers each provider's DoH template with `Add-DnsClientDohServerAddress` and per interface `DohInterfaceSettings` | Yes | Checked: Microsoft documents DoH client support in Windows 11 Settings and the `Add-DnsClientDohServerAddress` cmdlet (with `AutoUpgrade` and `AllowFallbackToUdp`) | Option on every preset that has a template; undo removes the registration |
| "Fastest" DNS | Benchmarks the eligible providers and applies the lowest latency one | Conditional | WinUtil times a TCP connect to port 53, which is not a DNS lookup. PCOptimizer's benchmark already measures real queries, so applying its winner is the better version | "Use the fastest" button on the Network page after an opt-in benchmark |
| Reset DNS to DHCP | "Default DHCP" choice | Covered | Undo restores the original servers | |
| Legacy F8 boot menu | `bcdedit /set bootmenupolicy legacy` and back to standard | Conditional | Checked: `bootmenupolicy` is documented in the BCDEdit reference; it is a boot configuration change, so Expert only under the rules | Better: a "Restart to advanced startup" button, `shutdown /r /o /t 0` (documented: "Goes to the Advanced boot options menu and restarts the device") |
| NTP pool | `w32tm /config /manualpeerlist:"pool.ntp.org,0x8" /syncfromflags:MANUAL` | Conditional | Checked: Microsoft's Windows Time tools page documents this exact form (with the 0x8 flag). Little benefit over `time.windows.com`, and it changes which server the PC contacts, so it must be listed in `docs/PRIVACY.md` | Low priority |
| Old Control Panel shortcuts | Opens Computer Management, Control Panel, mouse, network connections, power, printers, programs, region, security, sound, system properties, date and time, firewall, System Restore | Yes | Launches inbox tools from System32 | Small section on the Tools page; use full System32 paths only |
| WinGet repair | `Install-PackageProvider NuGet`, `Install-Module Microsoft.WinGet.Client`, `Repair-WinGetPackageManager -AllUsers` | Conditional | Covered in part (Apps page links to App Installer in the Store). The repair loads a module from the PowerShell Gallery into an elevated session, which the elevation rule forbids | Keep the Store link |
| AutoLogon | Downloads `Autologon.exe` from live.sysinternals.com and starts it | No | Stores the password as an LSA secret, weakens sign-in security, and downloads and runs an exe elevated | none |
| PowerShell profile | Installs the CTT PowerShell 7 profile | No | Out of scope; runs third-party script code in every shell | none |
| OpenSSH server | Installs and starts the OpenSSH server capability | No | Out of scope; opens remote access to the PC | none |
| Registry backup, legacy media, .NET, Hyper-V, WSL, Sandbox, Update reset, SFC and DISM | | Covered | | |

### 2.4 Install tab

| Feature | What WinUtil does | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|---|
| Uninstall apps | `winget uninstall --id <id> --source <source> --silent` | Yes | Checked: documented winget command; user-started | Uninstall button on the Apps page for installed catalog apps; reuse the trusted winget path and the ID allow list |
| Upgrade all apps | `winget upgrade --all --include-unknown --silent` | Conditional | Checked: `--all` upgrades every installed package and `--include-unknown` also those whose version cannot be determined, including apps the app has not reviewed | "Upgrade all catalog apps" using the existing `UpgradeArguments` per app, `--source winget` only |
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
| Automation | `-Config <file>` and `-Preset` (Standard, Minimal or Advanced) run every selected action without a window, with a time limit per step and a summary for the exit code | Conditional | A headless apply must not skip what the confirmation protects (Expert, Preview, boot-critical, anti-cheat). The current switches are read only by design | `--apply <export.json>` that refuses Expert and Preview items unless a second explicit switch is given, writes a report, and needs elevation like the UI |
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
| High contrast themes | App themes High Contrast Black and White | Yes | Accessibility; Windows contrast themes should be respected by WPF-UI | Check the app under Windows contrast themes first; add support where it breaks |
| Windows Insider notice | Warns that Insider builds require diagnostic data and offers to turn off conflicting settings | Yes | Simple check of the Insider registration | Finding that explains the conflict with `privacy.telemetryOff` |
| Global configuration lock and green or blue toggle colors | Padlock for settings enforced by an enterprise configuration file; color scheme for switches | No | Enterprise feature; colors would break the Fluent vocabulary rule | none |

### 3.2 Settings by category

IDs are O&O's where the manual names them. Every policy named here was checked in the Policy CSP reference; "Pro and higher" is what the page lists.

| O&O category and settings | Verdict | Reason | How to build in PCOptimizer |
|---|---|---|---|
| **App privacy with a policy:** camera, microphone, notifications, radios, unpaired devices, trusted devices, eye tracker, motion, voice activation (also above the lock screen), presence sensing (P082 to P085 in part), phone calls, screenshots of other windows and screenshot border | Conditional (opt-in) | Checked: Policy CSP Privacy documents `LetAppsAccessCamera`, `LetAppsAccessMicrophone`, `LetAppsAccessNotifications`, `LetAppsAccessRadios`, `LetAppsSyncWithDevices`, `LetAppsAccessTrustedDevices`, `LetAppsAccessGazeInput`, `LetAppsAccessMotion`, `LetAppsActivateWithVoice`, `LetAppsActivateWithVoiceAboveLock`, `LetAppsAccessHumanPresence`, `LetAppsAccessPhone`, `LetAppsAccessGraphicsCaptureProgrammatic` and `LetAppsAccessGraphicsCaptureWithoutBorder`, all Pro and higher. They apply to Store apps; desktop apps such as Discord follow the separate "Let desktop apps access" switch, which must be explained honestly | New "App permissions" group, never in a gaming recommendation |
| **App privacy without a policy:** documents, pictures, videos, file system, Bluetooth, USB, serial ports, Wi-Fi information, Wi-Fi Direct, generative AI, custom sensors (P086 to P094, P186 to P194 in part) | Conditional | Checked: Policy CSP Privacy has no policy for these. Only the Settings switches exist (stored under `CapabilityAccessManager\ConsentStore`), which PCOptimizer already uses for location as "the value behind a documented Windows option". The passkey policies (`LetAppsAccessPasskeys`, `LetAppsAccessPasskeysEnumeration`) are listed for Insider builds only | Same group, using the consent values with that note on the explanation page; passkeys only after they leave Insider builds |
| **Telemetry:** OneSettings downloads, device name in diagnostic data (U008), crash dump collection limit (P096) | Yes | Checked in Policy CSP System: `DisableOneSettingsDownloads` and `LimitDumpCollection` (Pro and higher, Windows 11 21H2 and later), `AllowDeviceNameInDiagnosticData` (Pro and higher), all under `Software\Policies\Microsoft\Windows\DataCollection` | Add to `12-privacy-core.json` |
| **Windows privacy:** cloud consumer account state content (P095) | Conditional | Checked: `DisableConsumerAccountStateContent` is listed for Enterprise and Education only | Edition gated like `privacy.consumerFeaturesOff` |
| **Location:** sensors, Windows location provider, location scripting | Yes | Checked: "Turn off sensors" (`DisableSensors`: "all programs on this computer can't use the sensor feature"), "Turn off Windows Location Provider", "Turn off location scripting", all Pro and higher under `Software\Policies\Microsoft\Windows\LocationAndSensors` | Separate tweaks next to `privacy.locationOff`; the sensors one is not for convertibles (screen rotation needs a VM test) |
| **Location:** remote connection location (P098) | Conditional | No matching policy found in the Policy CSP reference; the setting O&O writes is unknown | Only after a source is found |
| **Location:** location history | No | Checked: Microsoft deprecated and removed Location History in February 2025 | none |
| **Maps:** automatic map updates | No | Checked: the Maps app is deprecated and was removed from the Store in July 2025; the Maps platform APIs are deprecated | none |
| **Copilot and Windows AI:** Copilot, Copilot taskbar button, Copilot key, Recall enablement, AI data analysis (covered), Paint Image Creator, Cocreator, generative fill, Click to Do (C208), Settings agent (C209), AI in Notepad (C210), AI actions in File Explorer (C211) | Yes for Paint, Notepad and Recall enablement; Conditional for Copilot key and Click to Do; No for Settings agent on Pro | Checked, see 2.1. `TurnOffWindowsCopilot` (which also hides the taskbar button) is deprecated and does not apply to the new Copilot app; removing the app (already in Debloat) is the working route. The Copilot key policy can only point the key at another app. No policy was found for AI actions in File Explorer | Separate tweaks per feature, edition and build gated |
| **Search:** Microsoft account and work or school cloud search (M029, M030), device search history (M031), machine-wide Bing in Windows Search (M103) | Yes for M029 to M031, Conditional for M103 | Checked: `AllowCloudSearch` (Pro and higher) is already used by `privacy.cloudSearchOff`; Microsoft support documents the "Search history on this device" switch. `DoNotUseWebResults` is Enterprise and Education only; no other machine-wide web search policy was found | Extend the search tweaks with the device history switch |
| **Windows suggestions:** Start recommendations (M032), Start account notifications (M033), Settings account notifications (M034) | Yes for M033, Conditional for M032 and M034 | Checked: `DisableAccountNotifications` ("Turn off account notifications in Start", Pro and higher, 24H2 and later, user scope). M032 is `HideRecommendedSection` with the VM test from 2.2. No policy was found for M034 | Privacy group |
| **Office and Microsoft 365:** connected experiences that analyze content, download online content, optional connected experiences, all connected experiences, diagnostic data level, surveys, CEIP, first-run movie (F020), Office sign-in (F021) | Conditional | Checked: Microsoft documents `UserContentDisabled`, `DownloadContentDisabled`, `ControllerConnectedServicesEnabled`, `DisconnectedState` and `SendTelemetry` for Microsoft 365 Apps for enterprise and for business (and Project and Visio desktop). It does not say that consumer Microsoft 365 Personal or Family honor them. Turning off connected experiences removes features such as Designer and translation. Surveys, CEIP, F020 and F021 were not checked | Office group, opt-in, shown when Microsoft 365 Apps are installed; VM test with a consumer install first |
| **Edge (Chromium):** automatic sign-in from web to browser, visual search, form text prediction, cloud tab services, Microsoft Rewards | Yes | Checked: `ImplicitSignInEnabled`, `BrowserSignin`, `VisualSearchEnabled`, `TextPredictionEnabled`, `TabServicesEnabled` and `ShowMicrosoftRewards` exist with no managed device restriction | Same "Edge privacy" group |
| **Windows Explorer:** OneDrive file storage off, OneDrive network before sign-in, office.com files in Explorer (P099), recent items | Yes | Checked: "Prevent the usage of OneDrive for file storage" (`DisableFileSyncNGSC`, Pro and higher), `PreventNetworkTrafficPreUserSignIn` (OneDrive Group Policy reference), "Turn off account-based insights, recent, favorite, and recommended files in File Explorer" (`DisableGraphRecentItems`, Pro and higher, 22H2 and later), and the recent documents policies (`NoRecentDocsMenu`, `ClearRecentDocsOnExit`) | The OneDrive policy is a reversible alternative to the Debloat uninstall |
| **Windows Explorer:** sync provider notifications (OneDrive ads) | Conditional | A Folder Options checkbox; only third-party pages describe the value `ShowSyncProviderNotifications`, no Microsoft page found | After a Microsoft source is found |
| **Lock screen:** fun facts and tips, app notifications on the lock screen, lock screen camera | Yes | Checked: Microsoft support documents the "Fun facts, tips, tricks" option; "Turn off app notifications on the lock screen" (`DisableLockScreenAppNotifications`) and "Turn off toast notifications on the lock screen" are documented; "Prevent enabling lock screen camera" (`NoLockScreenCamera`) is documented, all Pro and higher | Privacy group |
| **Lock screen:** Windows Spotlight | Conditional | Checked: `AllowWindowsSpotlight` and the related Spotlight policies are Enterprise and Education only; only "Do not suggest third-party content in Windows spotlight" (`DisableThirdPartySuggestions`) is listed for Pro. "Do not show Windows tips" (`AllowWindowsTips`) is also Enterprise and Education only | Edition gated; the third-party suggestions one for Pro |
| **Windows Update:** optional and preview updates | Yes | Checked: `AllowOptionalContent` (value `SetAllowOptionalContent` under `Software\Policies\Microsoft\Windows\WindowsUpdate`) is listed for Pro and higher | Updates group in `15-updates-devices.json` |
| **Windows Update:** deferring of upgrades | Conditional | Checked: Pro and higher only (see 2.5) | Same as 2.5 |
| **Windows Update:** automatic Store app updates off | No | Checked: the policy exists ("Turn off Automatic Download and Install of updates", `WindowsStore\AutoDownload`, Pro and higher), but Store apps then miss security fixes | none |
| **Windows Update:** speech model updates off | Conditional | Checked: "Allow Automatic Update of Speech Data" (`AllowSpeechModelUpdate`, Pro and higher) covers speech recognition and speech synthesis models. Low value: Windows speech recognition is deprecated (December 2023) | Low priority |
| **Security, privacy related:** Windows Media DRM Internet access, password reveal button, Steps Recorder | Yes (password reveal, Steps Recorder), Conditional (DRM) | Checked: "Do not display the password reveal button" (`DisablePasswordReveal`), "Turn off Steps Recorder" (`DisableUAR`; Steps Recorder is deprecated but still present) and "Prevent Windows Media DRM Internet Access" (`DisableOnline`), all Pro and higher. The DRM policy stops license acquisition and DRM security upgrades; legacy DRM services are deprecated (September 2024), so it has little left to protect | Privacy group, opt-in |
| **Security, wireless:** NFC, wireless displays, mobile broadband, Wi-Fi Direct (S119), Bluetooth | No | Checked: `AllowBluetooth`, `AllowCellularData`, `AllowWiFiDirect`, `AllowConnectedDevices` and the Bluetooth CSP policies exist only as device management (MDM) policies with no Group Policy or registry mapping; `AllowNFC` is deprecated. A local app could only apply them through the MDM WMI bridge, and they break Miracast, Nearby Sharing, Wi-Fi Direct printing and, on some adapters, Mobile hotspot | none |
| **Mobile devices:** suggestions for using mobile devices | Conditional | A Settings switch; no Microsoft page or policy found | After a source is found |
| **Defender and SpyNet:** sample submission, SpyNet (MAPS) membership, malware infection reporting | No | Checked: the policies exist ("Join Microsoft MAPS", "Send file samples when further analysis is required", Pro and higher), but they lower cloud protection, which neither anti-cheats nor the security rules justify. With tamper protection on, "Cloud protection remains enabled" and "Attempts to modify Microsoft Defender Antivirus settings through the registry are blocked" | none |
| **Miscellaneous:** KMS client online validation off | No | Checked: "Turn off KMS Client Online AVS Validation" (`NoGenTicket`) only stops sending KMS client activation data to Microsoft. It does nothing on typical home and gaming PCs, which are activated with retail or OEM licenses, not KMS | none |
| **Miscellaneous:** network connectivity status indicator (active tests) off | No | Checked: "Turn off Windows Network Connectivity Status Indicator active tests" (`NoActiveProbe`): Microsoft says it "may reduce the ability of NCSI, and of other components that use NCSI, to determine Internet access"; captive portal sign-in in hotels and trains depends on it | none |
| **Activity history and clipboard, sync, Phone Link, feedback, tips, consumer features, Game Bar** | Covered | | |
| **Features Windows 11 no longer has:** Cortana (standalone app deprecated June 2023), Wi-Fi Sense (removed in Windows 10), Edge legacy (Do Not Track, form suggestions, search history, Edge bar; EdgeHTML no longer developed and not in Windows 11), People icon (deprecated in 1909, not in Windows 11), Timeline (retired in Windows 11) | No | The features no longer exist on Windows 11 24H2 or later, which is the app's minimum (deprecation dates from Microsoft's deprecated features page) | none |

---

## 4. Done by the competitors, refused by PCOptimizer on purpose

These are already explained with sources in `docs/not-included.md` and are not gaps:

- WinUtil Teredo off, IPv6 off, WPBT off, S0 network off, svchost split threshold, Disk Cleanup with `/ResetBase`, Windows Update off.
- WinUtil and O&O Defender sample submission off (O&O also SpyNet and malware reporting).
- WinUtil and O&O "Disable automatic Windows Updates".

Items added by this comparison that should go into `not-included.md` if you agree: Edge removal trick, BitLocker off, RDP warning off, Start menu feature flag, Store database lock, folder type discovery, UTC hardware clock, Razer and Logitech folder locks, `DisableCoInstallers`, Adobe hosts list, S3 sleep override, verbose blue screen values, MPO `DisableOverlays`, Windows AI end of life marking, Cross Device Experience Host removal, AutoLogon, Chocolatey, KMS validation, NCSI active tests, Store app auto updates off, Defender SpyNet and malware reporting, wireless MDM policies.

## 5. Recommended order

1. **Quick, documented, fits the profiles:** Paint and Notepad AI policies, `AllowRecallEnablement`, Sticky Keys and Filter Keys shortcuts, Reserved Storage, sign-in blur, long paths, NFS feature, `chkdsk /scan`, "Restart to advanced startup", telemetry extras (OneSettings, device name, dump limit), lock screen off.
2. **Biggest usability gaps:** export and import, text search on the Tweaks page, Uninstall and "Upgrade all catalog apps", undo grouped by session.
3. **Network:** more DNS presets, IPv6 addresses, DNS over HTTPS, "use the fastest" from the existing benchmark.
4. **Privacy breadth against O&O (needs your scope decision):** app permissions group, Edge privacy group, Brave group, location extras, lock screen items, OneDrive policies, account notifications, optional updates policy, password reveal, Steps Recorder.
5. **Cleanup:** Windows.old.
6. **Needs a VM test first:** Start recommendations policy on Pro, Office policies with consumer Microsoft 365, sensors and screen rotation, Num Lock value, Start Experiences App removal, `netsh int ip reset` together with DNS undo.
7. **Later, after code signing and an installer:** headless `--apply`, automatic re-apply after Windows updates.

Every new tweak still needs its EN and DE explanation page with the required sections, a summary of at most 200 characters and its sources before it ships. New network use (NTP pool, DoH resolvers) goes into `docs/PRIVACY.md` in both languages.

## 6. Decisions needed from you

- Whether the O&O style privacy expansion (3.2, groups marked opt-in) is in scope, given the CLAUDE.md rule on features modeled on O&O ShutUp10++.
- Whether PCOptimizer should get a "Personalize" group for Settings-only toggles (dark mode, taskbar, scrollbars, Snap) or stay strictly on optimization and privacy.
- Whether an installer and background re-apply (with its notifications, autostart and hybrid mode) are wanted at all, or the drift banner stays the only re-apply path.
- Whether more languages than English and German are planned.

## 7. Review log

**Second pass** (compared against every entry in WinUtil's configuration files, the action scripts and the O&O manual again; opened the Microsoft pages behind the edition claims):

- Corrected: DNS over HTTPS was described as missing in WinUtil; WinUtil sets DoH templates for every provider. Camera and Media Player were listed as missing from PCOptimizer's Debloat list; both are in it. Steps Recorder is deprecated, not removed.
- Verdicts changed: Settings agent and the Copilot app removal policy are Enterprise and Education only; Click to Do is listed for Insider builds only; Spotlight is Enterprise and Education only; Recall data deletion should use `AllowRecallEnablement`; Maps auto update and Location History are moot; PowerShell telemetry opt-out matters only before 7.6.2.
- Added: WinUtil MPO "Fully Disabled", two extra removable AppX packages, IPv6 DNS, "Fastest" DNS, the NFS security configuration, the FOSS badge, the headless run; O&O AI status overview, hybrid mode, notifications, autostart and tray, standard user mode, more languages, high contrast themes, P095.

**Third pass** (every remaining unchecked claim; section 8):

- Corrected: the lock screen policy is documented for Pro and higher (the second pass wrongly said it could not be confirmed); battery percentage is not a 22H2 feature but arrived in 2025 Insider builds and depends on the build; the KMS policy only stops sending activation data and does not affect license validation; the claim that some of WinUtil's Edge policies only work on managed devices was wrong for this set.
- Verdicts changed: Brave debloat Conditional to Yes; Notepad AI Conditional to Yes; lock screen off Conditional to Yes; Steps Recorder Conditional to Yes; account notifications in Start (M033) to Yes; battery percentage Yes to Conditional; `netsh int ip reset` Yes to Conditional (it wipes static IP and DNS settings); verbose blue screen Conditional to No; `DisableCoInstallers` Conditional to No; Cross Device Experience Host Conditional to No; wireless switches Conditional to No (MDM only); hiding the AI components Settings page to No; app permissions split into those with a policy and those with only a Settings switch; Edge debloat drops three obsolete or deprecated policies; Office policies limited to Microsoft 365 Apps for enterprise and business until a consumer install is tested.

## 8. Verification of claims

| Claim | Source opened | Result |
|---|---|---|
| Edge policies (17 from WinUtil, 6 from O&O) exist and have no managed device restriction | Edge browser policy reference, one page per policy; Edge Update policy reference | Confirmed; Collections obsolete after Edge 152; Wallet donation and search bar deprecated |
| Brave policies | brave-core `components/policy/resources/templates/policy_definitions/BraveSoftware` | All 9 Brave specific policies defined; the other 3 are Chromium policies |
| Reserved Storage command | DISM Reserved Storage command-line options; `Set-WindowsReservedStorageState` | Confirmed; can fail while servicing uses reserved storage |
| WindowsAI policies and editions | Policy CSP WindowsAI | Confirmed as stated in 2.1 |
| Notepad AI value | Microsoft Learn "Manage Notepad" | Confirmed: `HKLM\SOFTWARE\Policies\WindowsNotepad\DisableAIFeatures=1` |
| Settings page name `aicomponents` | Launch the Windows Settings app (ms-settings list) | Not listed |
| Notification policies | Policy CSP ADMX_Taskbar, ADMX_WPN | Confirmed, Pro and higher |
| Windows.old removal | Microsoft support "Delete your previous version of Windows" | Confirmed; automatic removal after 10 days |
| MPO registry values | NVIDIA article 5157 and its `mpo_disable.reg`, `mpo_restore.reg` | Only `OverlayTestMode`; `DisableOverlays` not used |
| Edge uninstall in the EEA | Microsoft EU policy blog, November 2023 and March 2024; Windows Insider blog, June 2025 | Confirmed for EEA region devices |
| RDP warnings | Microsoft Learn "Understanding security warnings when opening RDP files" | April 2026 security update; `RedirectionWarningDialogVersion` documented as a temporary revert |
| `DisableCoInstallers`, `RealTimeIsUniversal`, `PlatformAoAcOverride`, `DisplayParameters` | Searches of Microsoft Learn and support | No Microsoft documentation |
| Cross Device Experience Host, Start Experiences App | Searches; community answers only | No Microsoft page |
| Sticky, Filter and Toggle Keys; keyboard delay; window snapping | `STICKYKEYS` structure, `SystemParametersInfo` reference | Confirmed (`SKF_HOTKEYACTIVE`, `SPI_SETSTICKYKEYS`, `SPI_SETFILTERKEYS`, `SPI_SETTOGGLEKEYS`, `SPI_SETKEYBOARDDELAY`, `SPI_SETWINARRANGING`) |
| Battery percentage | Windows Insider blog (builds 26120.3000, 27802, 26100.3321) | Settings toggle from 2025, rollout paused once; registry value undocumented |
| Long paths, verbose status, clear logon background, lock screen off, Settings page visibility | Policy CSP ADMX_FileSys, ADMX_Logon, ADMX_ControlPanelDisplay, Settings; "Maximum path length limitation" | Confirmed, Pro and higher; long paths only for `longPathAware` apps |
| Num Lock value | Archived Windows 2000 registry reference; Microsoft Q&A | HKCU value documented (old); `.DEFAULT` community only |
| NFS client | Microsoft NFS overview | Client for NFS on all supported Windows client versions |
| `netsh int ip reset` | Microsoft "Reset TCP/IP by using the NetShell utility" | Confirmed; rewrites TCP/IP and DHCP parameters |
| `chkdsk /scan /perf`, `shutdown /r /o`, `bcdedit bootmenupolicy`, `w32tm /config` | Windows command references, BCDEdit reference, Windows Time tools page | Confirmed |
| DNS over HTTPS | "DNS over HTTPS client support", `Add-DnsClientDohServerAddress` | Confirmed |
| DNS addresses | Cloudflare 1.1.1.1 setup page; AdGuard DNS provider list | Confirmed; OpenDNS taken from AdGuard's list |
| `winget uninstall`, `winget upgrade --all --include-unknown` | winget command reference | Confirmed |
| App permission policies | Policy CSP Privacy (every policy) | 14 with a policy, Pro and higher; passkeys Insider only; no policy for files, Bluetooth, USB, serial, Wi-Fi information, Wi-Fi Direct, generative AI |
| Location, sensors | Policy CSP ADMX_Sensors, ADMX_LocationProviderAdm | Confirmed, Pro and higher |
| Search policies | Policy CSP Search; Microsoft support "Turn search history off or on" | `AllowCloudSearch` Pro and higher; `DoNotUseWebResults` Enterprise and Education only; device history switch documented |
| Account notifications, File Explorer recent and cloud files, OneDrive | Policy CSP Notifications, FileExplorer, ADMX_StartMenu, System; OneDrive Group Policy reference | Confirmed |
| Office privacy policies | "Use policy settings to manage privacy controls for Microsoft 365 Apps for enterprise" | Documented for enterprise and business editions only |
| Lock screen items, Spotlight, Windows tips | Policy CSP WindowsLogon, ADMX_WPN, DeviceLock, Experience; Microsoft support "Customize the lock screen" | Confirmed; Spotlight and tips Enterprise and Education only |
| Windows Update optional content, deferral, Store auto update | Policy CSP Update, ApplicationManagement | Confirmed, Pro and higher |
| Speech model updates, WMDRM, password reveal, Steps Recorder | Policy CSP Speech, ADMX_WindowsMediaDRM, CredentialsUI, ADMX_AppCompat | Confirmed, Pro and higher |
| Wireless policies | Policy CSP Connectivity, WiFi, Bluetooth | MDM only; `AllowNFC` deprecated |
| Defender MAPS and samples | Policy CSP Defender; tamper protection overview | Confirmed |
| KMS validation | Policy CSP Licensing | Only stops sending KMS activation data |
| NCSI active tests | Policy CSP Connectivity | Confirmed with Microsoft's warning |
| Sync provider notifications, mobile device suggestions, remote connection location (P098), AI actions in File Explorer, Office surveys, CEIP, F020, F021 | Searches | Not confirmed; left Conditional |

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

Microsoft reference pages:

- Policy CSP reference (Privacy, WindowsAI, Experience, Start, System, Update, Search, Notifications, FileExplorer, Settings, Speech, CredentialsUI, Connectivity, WiFi, Bluetooth, DeviceLock, WindowsLogon, Licensing, Defender, ApplicationManagement and the ADMX_ pages for ControlPanelDisplay, Sensors, LocationProviderAdm, Logon, FileSys, Taskbar, WPN, StartMenu, AppCompat, WindowsMediaDRM, ICM, DeviceSetup): <https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-configuration-service-provider>
- Manage Notepad: <https://learn.microsoft.com/en-us/windows/client-management/manage-notepad>
- Launch the Windows Settings app: <https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings-app>
- Tamper protection overview: <https://learn.microsoft.com/en-us/defender-endpoint/prevent-changes-to-security-settings-with-tamper-protection>
- Deprecated features in the Windows client: <https://learn.microsoft.com/en-us/windows/whats-new/deprecated-features>
- PowerShell about_Telemetry: <https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_telemetry>
- DISM Reserved Storage options: <https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-storage-reserve>
- Maximum path length limitation: <https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation>
- STICKYKEYS structure: <https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-stickykeys>
- SystemParametersInfo: <https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow>
- chkdsk: <https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/chkdsk>
- shutdown: <https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/shutdown>
- BCDEdit /set: <https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set>
- Windows Time service tools and settings: <https://learn.microsoft.com/en-us/windows-server/networking/windows-time-service/windows-time-service-tools-and-settings>
- Reset TCP/IP by using the NetShell utility: <https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/reset-tcp-ip-net-shell>
- DNS over HTTPS client support: <https://learn.microsoft.com/en-us/windows-server/networking/dns/doh-client-support>
- Add-DnsClientDohServerAddress: <https://learn.microsoft.com/en-us/powershell/module/dnsclient/add-dnsclientdohserveraddress>
- NFS overview: <https://learn.microsoft.com/en-us/windows-server/storage/nfs/nfs-overview>
- winget uninstall: <https://learn.microsoft.com/en-us/windows/package-manager/winget/uninstall>
- winget upgrade: <https://learn.microsoft.com/en-us/windows/package-manager/winget/upgrade>
- OneDrive Group Policy: <https://learn.microsoft.com/en-us/sharepoint/use-group-policy>
- Microsoft 365 Apps privacy controls: <https://learn.microsoft.com/en-us/microsoft-365-apps/privacy/manage-privacy-controls>
- Understanding security warnings when opening RDP files: <https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/remotepc/understanding-security-warnings>
- Delete your previous version of Windows: <https://support.microsoft.com/en-us/windows/deployment/install-upgrade/delete-your-previous-version-of-windows>
- Customize the lock screen: <https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-lock-screen-in-windows>
- Turn search history off or on: <https://support.microsoft.com/en-us/topic/turn-search-history-off-or-on-b0f77f8c-5235-4bea-93a7-c93733329979>
- Windows Insider blog, build 26120.3000 (battery percentage): <https://blogs.windows.com/windows-insider/2025/01/24/announcing-windows-11-insider-preview-build-26120-3000-dev-channel/>
- Microsoft EU policy blog, DMA compliance (Edge uninstall in the EEA): <https://blogs.microsoft.com/eupolicy/2024/03/07/microsoft-dma-compliance-windows-linkedin/>
- Edge browser policy reference: <https://learn.microsoft.com/en-us/deployedge/microsoft-edge-browser-policies>
- Edge Update policy reference: <https://learn.microsoft.com/en-us/deployedge/microsoft-edge-update-policies>

Other vendors:

- Brave policy definitions in brave-core: <https://github.com/brave/brave-core/tree/master/components/policy/resources/templates/policy_definitions/BraveSoftware>
- Brave Group Policy help page: <https://support.brave.app/hc/en-us/articles/360039248271-Group-Policy>
- NVIDIA article 5157 (MPO): <https://nvidia.custhelp.com/app/answers/detail/a_id/5157>
- Cloudflare 1.1.1.1 setup: <https://developers.cloudflare.com/1.1.1.1/setup/>
- AdGuard DNS providers list: <https://adguard-dns.io/kb/general/dns-providers/>
