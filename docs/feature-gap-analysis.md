# Feature gap analysis: Tunero and 17 tweak tools

Compared on 2026-10-11 against Tunero 0.6 (211 catalog tweaks, 48 checks, Debloat, Cleanup, Startup, Services and tasks, Apps and drivers, Tools, Health, Network, Changes). Input: `docs/Windows_Optimizer_Tools_Feature_Reference.md` (25,995 lines, 17 programs), a local working file that is not part of the repository. Line numbers (L1234) point into that file.

## How it was sorted

The reference file was split into 3,124 entries: every tweak heading of the tool sections plus every row of the catalogue tables (O&O ShutUp10++, Winaero Tweaker, Autoruns, Process Lasso, app and bloatware lists). Registry key and value name, service and task of each entry were matched automatically against the actions of the 211 catalog tweaks, then every entry was reviewed by hand, also against the pages that are not catalog tweaks (Debloat list, Services list, Startup locations, quick fixes, Windows features). Header rows of tables were skipped. docs/not-included.md and the earlier review of 2026-10-10 were not used as reasons: features they exclude were judged again on their effect, and a few quick checks of current Microsoft pages settled obsolete Edge policies and edition limits. One entry can carry more than one verdict, for example "already there" for its main value and "could implement" for a missing extra value.

| Verdict | Meaning |
|---|---|
| Already in Tunero | Tunero has the feature or an equivalent (often a documented policy where the tool writes the value behind a Settings switch). |
| Need to implement | A gap in something Tunero already offers, a documented fix for an open Preview or TODO item, a likely bug, or a feature most tools have that Tunero lacks. |
| Could implement | Has a real effect and fits the rules in CLAUDE.md, but is cosmetic, niche or low impact. "Needs proof" means an undocumented value (allowed only with proof, else Preview). |
| Edge case | Only for a small group (certain hardware, Office or Narrator users, managed or shared PCs, Copilot+ PCs, dual boot), or the effect depends on firmware. |
| Not sensible | Lowers security, only applies to Windows 10 or removed features, needs a program running in the background, is a hack that can break Windows, has no measurable effect, or is tool-internal. |

Verdicts are assessments. Before anything is added, the CLAUDE.md rules apply: open every source, write the EN and DE pages, mark undocumented values.

## Summary

Entries per tool and verdict. An entry with two verdicts counts in both columns. The app installer lists are counted in the last column only and summarized in their own section.

| Tool | Entries | Already | Need | Could | Edge | Not | Installer list |
|---|---|---|---|---|---|---|---|
| WinUtil | 136 | 82 | 0 | 30 | 9 | 19 | 236 |
| Win11Debloat | 215 | 155 | 2 | 34 | 5 | 23 |  |
| O&O ShutUp10++ | 291 | 188 | 3 | 50 | 17 | 38 |  |
| Winhance | 482 | 206 | 9 | 149 | 33 | 87 | 192 |
| Sophia Script | 120 | 70 | 1 | 26 | 12 | 13 |  |
| Sparkle | 49 | 34 | 0 | 4 | 1 | 11 | 162 |
| Winaero Tweaker | 87 | 22 | 0 | 26 | 17 | 26 |  |
| Autoruns | 19 | 17 | 0 | 0 | 1 | 1 |  |
| Process Lasso | 25 | 4 | 0 | 4 | 8 | 9 |  |
| AtlasOS | 274 | 109 | 2 | 63 | 35 | 84 |  |
| ReviOS | 88 | 46 | 0 | 12 | 12 | 32 |  |
| Bulk Crap Uninstaller | 43 | 7 | 0 | 15 | 5 | 16 |  |
| NVCleanstall | 10 | 2 | 0 | 0 | 2 | 6 |  |
| DDU | 0 | | | | | | |
| RemoveWindowsAI | 17 | 4 | 1 | 5 | 1 | 9 |  |
| CrapFixer | 105 | 85 | 2 | 8 | 4 | 7 |  |
| Flyoobe | 119 | 88 | 3 | 12 | 6 | 11 | 365 |
| **All** | **2080** | **1119** | **23** | **438** | **168** | **392** | **955** |

Grouped into features (the same setting in several tools counts once): already in Tunero 233, need to implement 7, could implement 127, edge cases 100, not sensible 176. Many already-in rows are small groups of related settings.

DDU has no entries of its own (its sections are prose); its features are in "Features without their own entry" below.

## Need to implement

| Feature | Why and how | References |
|---|---|---|
| Export and import of a selection | Save the applied tweaks (and optionally Debloat, services, start priorities) to a file and apply that file on another PC or after a reinstall, with the same confirmation and undo as "Apply recommended". Seven of the 17 tools have it: WinUtil (Import/Export, -Config), O&O ShutUp10++ (.cfg), Winhance (configuration), Sophia Script (preset file), Winaero Tweaker (.ini), Process Lasso (rule import and export), Flyoobe (recipes); BCU has uninstall lists. Tunero can only apply one PC at a time. | WinUtil L2238; Winaero Tweaker L14546; Sophia Script L9928; Process Lasso L14688; Flyoobe L24641; Bulk Crap Uninstaller L23218 |
| Game DVR policy AllowGameDVR | documented policy; done as gpu.gameDvrPolicyOff (Pro and higher). gpu.gameDvrOff is no longer Preview: the VM showed it Applied after apply, "Partial" appears only before apply on a fresh install | Win11Debloat L2667; O&O ShutUp10++ L4199; Winhance L5533; AtlasOS L15980; CrapFixer L24246; Flyoobe L24894 |
| Silent app installs off on Home and Pro | privacy.consumerFeaturesOff is marked not applicable on Home and Pro (Microsoft supports DisableWindowsConsumerFeatures only on Enterprise and Education since Windows 10 1607), so Home and Pro users have no way to stop promoted app installs in PCO today. Eleven of the 17 tools write the ContentDeliveryManager values SilentInstalledAppsEnabled, PreinstalledAppsEnabled, PreinstalledAppsEverEnabled and OemPreinstalledAppsEnabled; undocumented, so prove it in the VM with a new user profile (Preview until then) | O&O ShutUp10++ L4185; Winhance L4394, L4405, L4416, L4427; Sophia Script L10229; CrapFixer L24343; Flyoobe L24991 |
| App permissions: generative AI and system AI models | LetAppsAccessGenerativeAI and LetAppsAccessSystemAIModels (Privacy policies); the AI group has Recall, Paint and Notepad but not these two | O&O ShutUp10++ L3833; Winhance L4864, L4874; RemoveWindowsAI L23392 |
| Paint AI: generative erase and remove background | DisableGenerativeErase, DisableRemoveBackground are documented Paint policies; ai.paintOff is incomplete without them | Win11Debloat L2636; Winhance L4922, L4931 |
| Lock screen tips policy | LockScreenOverlaysDisabled (Personalization policy); could replace the undocumented values of focus.lockScreenTipsOff that also turn off Spotlight (docs/TODO.md); verify in the ADMX first | AtlasOS L17226 |
| PC Manager package name | Flyoobe lists Microsoft.MicrosoftPCManager, appx.json has Microsoft.PCManager; check the real package name, the entry may never match | Flyoobe L25283 |

## Could implement

Sorted by area, then by how many tools offer it. "Often requested" marks features found in three or more tools. The installer catalogue is listed separately at the end of this section.

### Gaming and graphics

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| Fullscreen optimizations off per game | the Compatibility checkbox per game exe (AppCompatFlags Layers DISABLEDXMAXIMIZEDWINDOWEDMODE), offered like the NVIDIA profiles per game; needs proof of the registry form | needs proof | 2 | Winhance L5378; ReviOS L22649 |
| Game Bar opens with the controller Xbox button off | UseNexusForGameBarEnabled, value behind the Game Bar switch; needs proof; common controller annoyance | needs proof | 2 | Winhance L5544; AtlasOS L15980 |
| Game Bar startup tips off | ShowStartupPanel; needs proof | needs proof | 2 | Winhance L5553; Sophia Script L12370 |
| NVIDIA Control Panel telemetry off | OptInOrOutPreference; undocumented, needs proof; NVIDIA systems | needs proof | 2 | AtlasOS L17052; ReviOS L21961 |
| NVIDIA: more documented driver settings | image scaling, FXAA, ambient occlusion, a global frame rate limit; only values from NvApiDriverSettings.h (documented) | documented | 1 | Sparkle L13853 |

### Windows AI and Copilot

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| Copilot and Recall taskbar pins off | TaskbarCompanion, CopilotPWAPin, RecallPin; needs proof | needs proof | 2 | Winhance L9313, L9322, L9331; RemoveWindowsAI L23392 |
| Copilot key remap | SetCopilotHardwareKey (documented WindowsAI policy); laptops with a Copilot key | documented | 2 | Winhance L4828; RemoveWindowsAI L23392 |
| Windows AI agent policies | DisableAgentConnectors, DisableAgentWorkspaces, DisableRemoteAgentConnectors, DisableSettingsAgent; check the WindowsAI Policy CSP first | to check | 2 | Winhance L4801, L4810, L4819; RemoveWindowsAI L23392 |
| Windows AI tasks off | Recall configuration tasks; Recall policies already stop Recall |  | 2 | Winhance L6317; RemoveWindowsAI L23401 |
| AI actions in File Explorer off | O&O C211; look for a documented policy first | to check | 1 | O&O ShutUp10++ L3828 |
| AI in the search box off | M025; mechanism to be found | to check | 1 | O&O ShutUp10++ L4168 |
| AI service (WSAIFabricSvc) to Manual | add to services.json so the Services page offers it |  | 1 | Win11Debloat L2575 |
| Chrome: more AI policies | AutofillPredictionSettings, DevToolsGenAiSettings, CreateThemesSettings and others; GenAiDefaultSettings covers most |  | 1 | Win11Debloat L2608 |
| Copilot nudges off | ShowCopilotNudges; undocumented, needs proof | needs proof | 1 | Winhance L4949 |

### Privacy, suggestions and notifications

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| More suggestion switches (often requested) | values behind Settings switches the tools add to the suggestion set: backup reminder and suggested notifications, Mobility OptedIn, ContentDeliveryAllowed and FeatureManagementEnabled master values; the silent install values are a Need row |  | 5 | Win11Debloat L2319; O&O ShutUp10++ L4162; Winhance L4350, L4361, L4372; AtlasOS L15517; ReviOS L21826 |
| Edge: more policies (often requested) | documented Edge policies: ShowMicrosoftRewards, WalletDonationEnabled, MicrosoftEdgeInsiderPromotionEnabled, EdgeAssetDeliveryServiceEnabled, ConfigureDoNotTrack, ComposeInlineEnabled, DevToolsGenAiSettings, ShareBrowsingHistoryWithCopilotSearchAllowed, Edge Secure Network; check AllowBrowsingWithCopilot and CopilotNewTabPageEnabled against the policy list | documented | 4 | WinUtil L525; O&O ShutUp10++ L3994, L3995, L4008, L4009, L4020, L4021; Winhance L5021, L5057, L5066; RemoveWindowsAI L23392 |
| Lock screen slideshow off | SlideshowEnabled, value behind the Settings choice | needs proof | 2 | CrapFixer L24314; Flyoobe L24962 |
| Settings account notifications off | M034; value behind the Settings switch | needs proof | 2 | O&O ShutUp10++ L3831; Winhance L4513 |
| Speech model updates off | Speech AllowSpeechModelUpdate policy (documented) | documented | 2 | O&O ShutUp10++ L4084, L4124; AtlasOS L16183 |
| Telemetry tasks: more | Feedback DmClient (2 tasks), WER QueueReporting, PI Sqm-Tasks, MareBackup, StartupAppTask, PcaPatchDbTask, Flighting UsageDataReporting, Power Efficiency Diagnostics AnalyzeSystem |  | 2 | Winhance L6247, L6254, L6261, L6268, L6275, L6282, L6310; AtlasOS L15611, L18844 |
| App permissions: libraries and file system | documents, pictures, videos, music, downloads, file system: only user consent values, no LetApps policy; needs proof | needs proof | 1 | O&O ShutUp10++ L3914, L3915, L3916, L3917, L3918, L3919, L3920, L3921, L3932, L3933, L3934, L3935 |
| App permissions: more capabilities | human presence, eye tracking (LetAppsAccessGazeInput) and graphics capture (LetAppsAccessGraphicsCaptureProgrammatic, WithoutBorder) are documented LetApps policies | documented | 1 | O&O ShutUp10++ L3833, L3924, L3925, L3926, L3927, L3928, L3929, L3930, L3931 |
| Brave: more policies | TorDisabled, MetricsReportingEnabled, UrlKeyedAnonymizedDataCollectionEnabled, SafeBrowsingExtendedReportingEnabled; documented Chromium policies | documented | 1 | WinUtil L495 |
| Cross-device resume off | Connectivity DisableCrossDeviceResume policy (documented) | documented | 1 | AtlasOS L16436 |
| Desktop Spotlight off | DisableSpotlightCollectionOnDesktop policy (documented); privacy.spotlightOff covers the lock screen | documented | 1 | Win11Debloat L2481, L2495 |
| Device name out of diagnostic data | AllowDeviceNameInTelemetry policy (documented); default already off | documented | 1 | O&O ShutUp10++ L3832 |
| Edge: autofill policies | card and form data |  | 1 | O&O ShutUp10++ L3960, L3961, L3964, L3965, L3966, L3967 |
| Feature advertisement balloons off | NoBalloonFeatureAdvertisements, NoAutoTrayNotify (documented Explorer policies) | documented | 1 | ReviOS L21126 |
| Internet wizards off | NoOnlinePrintsWizard, NoPublishingWizard, NoWebServices (documented Explorer policies) | documented | 1 | ReviOS L21961 |
| No controlled feature rollouts | P017; find the documented value (AllowTemporaryEnterpriseFeatureControl is the opposite direction) | to check | 1 | O&O ShutUp10++ L4129 |
| Reminders and calls on the lock screen off | value behind the Settings switch | needs proof | 1 | Winhance L7334 |
| Settings Home banner off | ValueBanner activation; undocumented, needs proof | needs proof | 1 | AtlasOS L16542 |
| Settings sync per category | documented SettingSync policies (themes, passwords, language, accessibility); full sync off exists | documented | 1 | O&O ShutUp10++ L4068, L4069, L4070, L4071, L4072, L4073 |
| Single system notifications off | capability access notifications |  | 1 | Winhance L7396 |
| Touch keyboard text suggestions off | value behind the Settings switch | needs proof | 1 | O&O ShutUp10++ L3862 |

### Windows Update

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| Not first in line for updates (often requested) | IsContinuousInnovationOptedIn, value behind the Settings switch (off by default) | needs proof | 3 | Win11Debloat L3603; Winhance L7201; Sophia Script L11767 |
| Notify when a restart is required (often requested) | RestartNotificationsAllowed2, value behind the Settings switch; needs proof | needs proof | 3 | Winhance L7252; Sophia Script L11682; AtlasOS L18914 |
| Quality updates deferral | DeferQualityUpdatesPeriodInDays policy (documented); few days of delay | documented | 2 | WinUtil L1742; AtlasOS L19038 |
| Active hours | ActiveHoursStart/End policies (documented); keeps update restarts out of play time | documented | 1 | Sophia Script L11736 |
| No wake for updates | AUPowerManagement policy (documented) | documented | 1 | AtlasOS L18016 |
| Optional and preview updates off | AllowOptionalContent policy (documented); default is opt-in | documented | 1 | O&O ShutUp10++ L4130 |
| Stay on the current Windows version | TargetReleaseVersion and ProductVersion policies (documented); PCO could fill in the installed version; the user has to lift it to get the next feature update | documented | 1 | AtlasOS L18081 |
| Update and shut down not the default | NoAUAsDefaultShutdownOption policy (documented) | documented | 1 | AtlasOS L18151 |

### Explorer and context menu

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| Explorer: folders back in This PC (often requested) | the six user folders under This PC (HiddenByDefault or NameSpace keys, ThisPCPolicy); undocumented, needs proof | needs proof | 5 | Win11Debloat L3424; Winhance L8380, L8392, L8404, L8416, L8428, L8440, L8452, L8464, L8476, L8488, L8500, L8512; Winaero Tweaker L14608; AtlasOS L17407; Bulk Crap Uninstaller L23276, L23282, L23290, L23298, L23306, L23314 |
| Explorer: hide Home and Gallery (often requested) | System.IsPinnedToNameSpaceTree on the Home and Gallery CLSIDs; undocumented, needs proof | needs proof | 5 | WinUtil L639; Win11Debloat L3329, L3352; Winhance L8536, L8549; AtlasOS L17429, L19288; ReviOS L22865, L22882 |
| No "- Shortcut" suffix on new shortcuts (often requested) | NamingTemplates ShortcutNameTemplate (or the older link value); cosmetic; shortcut arrow removal stays out |  | 4 | Winhance L7529; Sophia Script L10973; Winaero Tweaker L14607; AtlasOS L17601, L19425 |
| Windows Photo Viewer back (often requested) | registers Photo Viewer file associations and capabilities so it shows in Open with; harmless; the default app stays the user choice |  | 4 | Winhance L8148; Winaero Tweaker L14626; ReviOS L21510; RemoveWindowsAI L23858 |
| Drive letters before labels (often requested) | ShowDriveLettersFirst; cosmetic |  | 3 | Win11Debloat L3544, L3559, L3572, L3587; Winhance L8290; Winaero Tweaker L14610 |
| Explorer: Folder Options switches (often requested) | browse folders, single click, always show menus, file icon on thumbnails, size in folder tips, hide empty drives, merge conflicts, separate process, restore windows, NTFS colors, infotips, preview handlers, status bar, sharing wizard, type-ahead, .lnk extension; preferences that Folder Options already offers |  | 3 | Winhance L7963, L7979, L8034, L8052, L8088, L8117, L8137, L8254, L8272, L8281, L8301, L8310, L8319, L8328, L8355, L8364; Sophia Script L10406, L12101; AtlasOS L19239 |
| Explorer: Quick Access privacy (often requested) | ShowRecent, ShowFrequent, ShowCloudFilesInQuickAccess, HubMode, ClearRecentDocsOnExit; values behind Folder Options switches plus Explorer policies |  | 3 | Winhance L7997, L8007, L8016; Sophia Script L10561, L10588; AtlasOS L17475, L17511, L19304 |
| Explorer: no duplicate removable drives (often requested) | DelegateFolders key; undocumented, needs proof | needs proof | 3 | Win11Debloat L3316; Winhance L8674; AtlasOS L17573, L19320 |
| Shortcuts to classic Control Panel applets (often requested) | one card with links (compmgmt, ncpa, mmsys, sysdm, powercfg.cpl and so on); low value |  | 3 | WinUtil L1548, L1557, L1566, L1575, L1584, L1593, L1602, L1611, L1620, L1629, L1638, L1647, L1656, L1665; Winaero Tweaker L14636; AtlasOS L16613 |
| Context menu for more than 15 files | MultipleInvokePromptMinimum |  | 2 | Winaero Tweaker L14602; AtlasOS L17502 |
| Detailed file copy dialog | EnthusiastMode; undocumented | needs proof | 2 | Sophia Script L10507; AtlasOS L17365 |
| Explorer folder type discovery off | resets Bags and sets FolderType NotSpecified; undocumented, needs proof; fixes slow Downloads folder | needs proof | 2 | WinUtil L434; AtlasOS L15929, L19255 |
| Explorer: navigation pane folders | Desktop, Documents, Downloads, Music, Pictures, Videos, Libraries |  | 2 | Winhance L8591, L8603, L8615, L8627, L8639, L8651, L8663; AtlasOS L17457, L18634 |
| Explorer: navigation pane options | availability status, expand to folder, show all folders |  | 2 | Winhance L8562, L8571, L8580; Sophia Script L11176 |
| Remove default context menu entries | Share, Cast to device, Give access, Include in library, Restore previous versions, Open in Terminal, Edit with Photos, Print, Troubleshoot compatibility, zip Extract, New Bitmap and Rich Text; Shell Extensions Blocked or ProgrammaticAccessOnly; needs proof per entry | needs proof | 2 | Winaero Tweaker L14603; AtlasOS L17583, L18216, L18225, L18232, L18241, L18250, L18278, L18285, L18295, L18608, L19101 |
| Default drag and drop action | DefaultDropEffect; undocumented | needs proof | 1 | Winaero Tweaker L14609 |
| Downloads folder without grouping | FolderTypes TopViews GroupBy; undocumented | needs proof | 1 | Sophia Script L11149 |
| Explorer address bar auto completion | AutoComplete Append Completion |  | 1 | Winaero Tweaker L14611 |
| Explorer start folder Downloads | LaunchTo=3, value behind the Explorer setting | needs proof | 1 | Win11Debloat L3518 |
| Explorer: hide OneDrive in navigation | not needed after OneDrive uninstall |  | 1 | Win11Debloat L3374 |
| Explorer: no Office.com files in Home | value behind the Folder Options switch; needs proof | needs proof | 1 | O&O ShutUp10++ L3837 |
| F1 key does not open the help page | TypeLib override; undocumented, needs proof | needs proof | 1 | Sophia Script L11951 |
| Hide Open in Terminal in the context menu | Shell Extensions Blocked key; cosmetic |  | 1 | Winhance L7721 |
| Recycle Bin delete confirmation | ConfirmFileDelete policy (documented) | documented | 1 | Sophia Script L10534 |
| Send To menu cleanup | removes unused Send To items |  | 1 | AtlasOS L17385, L19150 |

### Taskbar, Start and desktop

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| Hide Settings Home page (often requested) | SettingsPageVisibility policy (documented) with hide:home; the same policy can hide the AI components page | documented | 5 | WinUtil L1184; Win11Debloat L2830; Winaero Tweaker L14606; AtlasOS L16595; RemoveWindowsAI L23399 |
| Wallpaper without JPEG compression (often requested) | JPEGImportQuality=100; undocumented, needs proof; cosmetic | needs proof | 5 | Winhance L5230; Sophia Script L10954; Winaero Tweaker L14598; AtlasOS L16331; Bulk Crap Uninstaller L23263 |
| Start layout (more pins or more recommendations) (often requested) | Start_Layout, value behind the Settings choice | needs proof | 4 | Winhance L8912; AtlasOS L17769; CrapFixer L24568; Flyoobe L25196 |
| Show all tray icons (often requested) | per-app IsPromoted values under NotifyIconSettings (EnableAutoTray is ignored on Windows 11); needs proof | needs proof | 3 | Winhance L9259, L9268; CrapFixer L24430; Flyoobe L25087 |
| Start All Apps view mode (often requested) | AllAppsViewMode (category, grid, list), value behind the choice in Start (26200+); cosmetic | needs proof | 3 | Win11Debloat L2783, L2797, L2813; Winhance L8929; Sophia Script L11241 |
| Unpin all taskbar apps (often requested) | Taskband Favorites; needs a backup for undo |  | 3 | Winhance L9046; Sophia Script L10822; AtlasOS L17860 |
| Window snapping off completely (often requested) | WindowArrangementActive, value behind the Settings switch | needs proof | 3 | WinUtil L1157; Win11Debloat L2970; Winaero Tweaker L14561 |
| Lock and sleep in the power menu | ShowLockOption, ShowSleepOption (Explorer policies) |  | 2 | Winhance L7116, L7127; ReviOS L22417 |
| Phone Link in Start off | per-user value behind the Start setting; needs proof | needs proof | 2 | Win11Debloat L2746; Flyoobe L25205 |
| Taskbar apps on multiple displays | MMTaskbarMode, Settings choice; needs proof | needs proof | 2 | Win11Debloat L3252, L3263, L3276; Winhance L9153, L9162 |
| Taskbar last active click | LastActiveClick; undocumented, needs proof | needs proof | 2 | Win11Debloat L3115; Winaero Tweaker L14594 |
| Taskbar transparency level | TaskbarAcrylicOpacity; undocumented, cosmetic | needs proof | 2 | Winhance L9360; Winaero Tweaker L14593 |
| Unpin all Start apps | needs a backup of start2.bin for undo; ConfigureStartPins locks the layout |  | 2 | Winhance L8825, L8873; Sophia Script L11222 |
| Battery percentage in the taskbar | IsBatteryPercentageEnabled, value behind the Settings switch; needs proof; laptop profiles | needs proof | 1 | WinUtil L1002 |
| Clock in the notification center | Settings switch value |  | 1 | Sophia Script L10770 |
| Desktop icons: Recycle Bin, user folder, Control Panel, Network | same mechanism as personalize.desktopThisPc |  | 1 | Winhance L7589, L7599, L7609, L7619 |
| Drag tray off | DragTrayEnabled, value behind the Settings switch (26200+); needs proof | needs proof | 1 | Win11Debloat L2862 |
| Hide Learn about this picture icon | HideDesktopIcons value; needs proof | needs proof | 1 | Win11Debloat L2511 |
| Notification bell icon hidden | ShowNotificationIcon; cosmetic |  | 1 | Winhance L7345 |
| Quick settings pinned items | cosmetic |  | 1 | AtlasOS L17744 |
| Share any window from the taskbar off | TaskbarSn, Settings switch |  | 1 | Winhance L9179 |
| Smaller taskbar buttons | IconSizePreference, Settings choice (24H2+) |  | 1 | Winhance L9231 |
| Start folders next to the power button | AllowPinnedFolder policies (documented Start CSP); Settings choice | documented | 1 | ReviOS L21210 |
| Task View hover to switch desktops off | undocumented, cosmetic | needs proof | 1 | Winaero Tweaker L14592 |
| Taskbar auto-hide | StuckRects3 binary; Settings switch |  | 1 | Winhance L9106 |
| Taskbar hover delay | ExtendedUIHoverTime; undocumented | needs proof | 1 | Winhance L9115 |
| Taskbar thumbnail size and delay | Taskband values; undocumented, cosmetic | needs proof | 1 | Winaero Tweaker L14597 |
| Windows version on the desktop | PaintDesktopVersion; cosmetic |  | 1 | Winaero Tweaker L14599 |

### Appearance and input

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| Menu and hover delays (often requested) | MenuShowDelay and MouseHoverTime (SystemParametersInfo values); menus and tooltips open sooner; cosmetic, no effect on games |  | 6 | Winhance L5151, L5239; Sparkle L13793; Winaero Tweaker L14568; AtlasOS L17546, L17682; CrapFixer L24383; Flyoobe L25031 |
| Appearance extras | Aero Lite theme, Alt+Tab look (undocumented values) | needs proof | 2 | Winaero Tweaker L14551, L14552, L14553, L14557; AtlasOS L17290 |
| More accessibility shortcuts off | Narrator (Win+Ctrl+Enter), Mouse Keys, High Contrast and the accessibility tool shortcut; same API as input.accessibilityShortcutsOff |  | 2 | Winhance L6538, L6574, L6583; AtlasOS L17312, L17337 |
| Num Lock on at sign-in | InitialKeyboardIndicators; no Microsoft page; needs proof | needs proof | 2 | WinUtil L1147; Sophia Script L11971 |
| Startup sound off | Settings switch value; cosmetic |  | 2 | Winhance L7455; ReviOS L22330 |
| Always show scrollbars | DynamicScrollbars, value behind the Accessibility switch; needs proof | needs proof | 1 | WinUtil L1117 |
| Control Panel icon view | cosmetic |  | 1 | Sophia Script L10854 |
| Default terminal app | Settings choice |  | 1 | Sophia Script L12169 |
| Require Ctrl+Alt+Del at sign-in | DisableCAD policy (documented), more secure sign-in | documented | 1 | Winaero Tweaker L14579 |
| Visual effects: single switches | taskbar thumbnail cache (AlwaysHibernateThumbnails); cosmetic |  | 1 | Winhance L6428, L6461, L8025 |

### Power, system and services

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| Detailed blue screen info (often requested) | CrashControl DisplayParameters (stop code parameters on the blue screen) and AutoReboot off; DisableEmoticon is undocumented; turning off crash dumps stays out | needs proof | 8 | WinUtil L992; Sophia Script L11512; Sparkle L12974; Winaero Tweaker L14571; AtlasOS L17820; ReviOS L22244; CrapFixer L24363; Flyoobe L25011 |
| Services list: more reviewed services (often requested) | Microsoft services the Services page shows read-only today: WerSvc, wercplsupport, diagnosticshub.standardcollector, WalletService, SCDeviceEnum, SCPolicySvc, WMPNetworkSvc, MixedRealityOpenXRSvc, icssvc, SmsRouter, SEMgrSvc, svsvc, RasMan, RasAuto, TermService, SessionEnv, UmRdpService, TapiSrv, SensrSvc, SensorDataService, OneSyncSvc, WSAIFabricSvc, dam, GpuEnergyDrv; each needs a note in services.json |  | 3 | Winhance L5685, L5770, L5840, L5857, L5874, L5891, L5925, L5942, L5959, L5976, L5993, L6010, L6027, L6144, L6161, L6178, L6195; AtlasOS L15363; ReviOS L20502 |
| Power options editor | display, disk, sleep and hibernate timeouts, hybrid sleep, power and sleep buttons, lid action, battery levels and actions; Windows has them in Settings and the classic power options |  | 2 | Winhance L6619, L6630, L6674, L6709, L6720, L6799, L6810, L6821, L7006, L7017, L7028, L7039, L7050, L7061, L7072; AtlasOS L18814 |
| Efficiency mode per program | needs a running program; Windows applies it only per process at run time |  | 1 | Process Lasso L14701 |
| Indexing respects power mode | Settings switch under Searching Windows; battery profile |  | 1 | AtlasOS L16017, L18784 |
| Maintenance wake-up off | Task Scheduler maintenance WakeUp policy (documented); laptop and quiet profiles | documented | 1 | AtlasOS L15886 |
| More removable capabilities | legacy Notepad and Paint capabilities |  | 1 | Winhance L9500, L9504, L9505 |
| Power plan switching while a game runs | IdleSaver and application power profiles; same resident component |  | 1 | Process Lasso L14693, L14695, L14696 |
| Recommended troubleshooting setting | Settings choice; low effect |  | 1 | Sophia Script L11902 |
| Restart apps after sign-in | RestartApps, Settings switch value |  | 1 | Sophia Script L12082 |

### Tools, pages and catalogues

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| More removable apps (often requested) | packages the tools remove that the Debloat list lacks: Microsoft.StartExperiencesApp, MicrosoftWindows.CrossDevice, Microsoft.WidgetsPlatformRuntime, Microsoft.Windows.AIHub, Microsoft.XboxGameOverlay, Microsoft.Wallet, Microsoft.WritingAssistant, Microsoft.Office.ActionsServer, WindowsWorkload AI packages, TikTok.TikTok, Twitter.Twitter, EclipseManager; each needs the usual review (guard, Store link) |  | 6 | WinUtil L2114, L2135; Win11Debloat L3686, L3731, L3770, L3780, L3786, L3788; Winhance L9466, L9482, L9484, L9485, L9486; RemoveWindowsAI L23395; CrapFixer L24604, L24630, L24633; Flyoobe L25279, L25308, L25311 |
| Icon cache reset | deletes the icon and thumbnail cache databases and restarts Explorer; no Microsoft procedure, but harmless and fixes broken icons |  | 2 | Winaero Tweaker L14634; Flyoobe L25753 |
| DNS: filtering variants and custom servers | Cloudflare malware and family, AdGuard family, Quad9 unfiltered, user-entered servers |  | 1 | WinUtil L961 |
| Defender signature update and status | Update-MpSignature (documented); quick fix | documented | 1 | Flyoobe L25787 |
| Leftovers after uninstall | scan folders, shortcuts and Program Files after an uninstall, files to the Recycle Bin |  | 1 | Bulk Crap Uninstaller L23195, L23196, L23197, L23198, L23199, L23201, L23202 |
| Orphaned programs in Program Files | find folders without an uninstall entry; review before delete |  | 1 | Bulk Crap Uninstaller L23170 |
| Pending restart check before changes | warn when Windows waits for a restart (PCO checks free space with F17) |  | 1 | Flyoobe L25920 |
| Repair winget | PCO links App Installer; a repair (Repair-WinGetPackageManager or re-register) is missing |  | 1 | WinUtil L1536 |
| Restart into Safe Mode | bcdedit safeboot with a safe way back; useful before DDU |  | 1 | AtlasOS L20097 |

### Other

| Feature | What it changes | Basis | Tools | References |
|---|---|---|---|---|
| WPBT execution off (often requested) | same value |  | 3 | WinUtil L388; Sparkle L14243; AtlasOS L17833 |
| Plain color sign-in background policy | DisableLogonBackgroundImage; verify it in the Windows 11 ADMX; personalize.logonBlurOff exists |  | 1 | Winaero Tweaker L14582 |

### App installer catalogue

WinUtil, Winhance, Sparkle and Flyoobe ship installer lists; about 569 names (with spelling variants) are not on Tunero's Apps page, which has 26 reviewed apps. A larger list fits the Apps page; every entry needs the same review as today (winget id, publisher, what it installs).

- Gaming related, the best fit: DirectX Runtime (June 2010 D3DX for older games), Visual C++ 2015-2022 x86 and the older 2005 to 2013 runtimes (older games), Playnite, Heroic Games Launcher, Itch.io, Rockstar Games Launcher, Minecraft Launcher, Prism Launcher, Modrinth App, CurseForge, GeForce NOW, PS Remote Play, Moonlight, Sunshine, Medal, Streamlabs OBS, CapFrameX, Intel PresentMon, HWMonitor, Crystal Disk Mark, Cinebench R23, Fan Control, OpenRGB, SignalRGB, Logitech G HUB, Razer Synapse 4, SteelSeries GG, Voicemeeter, EarTrumpet, Borderless Gaming, JoyToKey, Display Driver Uninstaller, NVCleanstall, NVIDIA App, Process Explorer, System Informer.
- General utilities found in two or more lists: Notepad++, PotPlayer, ShareX, Greenshot, Everything, WizTree, TreeSize Free, PowerShell 7, Windows Terminal, NanaZip, PeaZip, Brave, LibreWolf, Vivaldi, Bitwarden, KeePassXC, Signal, Telegram, LibreOffice, Sumatra PDF, qBittorrent, Rufus, Ventoy.
- Keep out: registry and system cleaners (CCleaner, Wise Registry Cleaner, Glary Utilities, IObit tools), other tweak tools (O&O ShutUp10++, NTLite, Process Lasso), shell replacements (ExplorerPatcher, StartAllBack, Open-Shell) and driver installers (Snappy Driver Installer Origin), for the same reasons as the matching Not sensible rows.

## Edge cases

| Feature | Who it is for, or why it is limited | Tools | References |
|---|---|---|---|
| Click to Do off | Copilot+ PCs only | 5 | Win11Debloat L2561; O&O ShutUp10++ L3828; Winhance L4783; CrapFixer L24098; Flyoobe L24746 |
| Lock screen off | NoLockScreen works only on Enterprise and Education | 5 | WinUtil L1211; Winaero Tweaker L14578; AtlasOS L19338; CrapFixer L24543; Flyoobe L25171 |
| No auto restart while signed in | NoAutoRebootWithLoggedOnUsers only works with scheduled installs (AUOptions 4); active hours cover it | 5 | Win11Debloat L3616; Winhance L7228; Winaero Tweaker L14566; AtlasOS L18016; Flyoobe L25227 |
| Hide Start Recommended section | HideRecommendedSection policy is for SE and Education; on Pro only through a fake education flag; PCO already empties it with four switches | 4 | WinUtil L1220; Win11Debloat L2718; Winhance L8946; Sophia Script L11306 |
| Narrator online services and scripting | Narrator users only | 3 | Winhance L4535, L4544; CrapFixer L24323; Flyoobe L24971 |
| Shorter shutdown timeouts | WaitToKillServiceTimeout and app timeouts; faster shutdown with hung apps, risk of lost data | 3 | AtlasOS L17780; CrapFixer L24401; Flyoobe L25049 |
| Updates for other Microsoft products | AllowMUUpdateService; only matters with Office; off delays Office security fixes | 3 | O&O ShutUp10++ L4132; Winhance L7210; Sophia Script L11656 |
| Boot logo, spinner and messages off | BCD appearance and behavior (always advanced options, automatic repair off); Expert | 2 | Winaero Tweaker L14576; AtlasOS L19559, L19568, L19594 |
| Caps Lock off | Scancode Map; keyboard remap for few users | 2 | Sophia Script L11990; ReviOS L22852 |
| Edge Copilot page context | applies to Entra ID profiles only | 2 | O&O ShutUp10++ L3996, L3997; Winhance L4967, L4976 |
| Edge Microsoft 365 Copilot icon | Entra ID profiles only | 2 | O&O ShutUp10++ L4006, L4007; Winhance L4994, L5003 |
| Edge swipe off | AllowEdgeSwipe policy; touch screens only | 2 | AtlasOS L19223; ReviOS L22793 |
| File sharing off | unbinds SMB client and server, NetBIOS off, all networks Public; breaks LAN shares | 2 | AtlasOS L15363, L18565; ReviOS L20502 |
| Force S3 sleep instead of Modern Standby | PlatformAoAcOverride; undocumented, depends on firmware, can leave no sleep at all | 2 | WinUtil L1175; ReviOS L23104 |
| Hardware clock in UTC | only for dual boot with Linux; RealTimeIsUniversal | 2 | WinUtil L581; Sparkle L14131 |
| Legacy Notepad for text files | needs the legacy notepad.exe | 2 | Winhance L8225; RemoveWindowsAI L23893 |
| Office AI and connected experiences | Office users only; documented Office policies | 2 | Winhance L5075, L5084, L5094, L5103, L5112, L5123; AtlasOS L17061 |
| OpenSSH server | admins only; could join the capability list | 2 | WinUtil L1706; Winhance L9507 |
| Other time server (pool.ntp.org) | time.windows.com works; PCO has Sync the clock now | 2 | WinUtil L1434; AtlasOS L15717 |
| Peripheral co-installer auto-install off (Razer) | DisableCoInstallers; only for owners of such devices | 2 | WinUtil L747; Winhance L7282 |
| Print Spooler off | for PCs without any printer: removes the spooler attack surface; printing and Print to PDF stop, and the spooler has no start trigger, so Manual acts like Disabled | 2 | Winhance L5616; AtlasOS L19806 |
| Radios off (NFC, mobile broadband, Wi-Fi Direct, Bluetooth pairing) | hardware specific | 2 | O&O ShutUp10++ L3945, L3947, L3948, L3949; AtlasOS L19671 |
| Settings agent off | C209; Copilot+ PCs only | 2 | O&O ShutUp10++ L3828; Winhance L4792 |
| Sign-in screen options | network icon, power button, last logon info; shared or managed PCs | 2 | Winaero Tweaker L14583, L14584, L14585; ReviOS L22417 |
| Thumbnail cache not cleaned automatically | only for big photo folders | 2 | Winhance L8078; Sophia Script L12061 |
| ms-gamebar protocol stub | stops the popup after Game Bar removal; undocumented | 2 | Win11Debloat L2682; ReviOS L20829 |
| .NET CLI telemetry off | DOTNET_CLI_TELEMETRY_OPTOUT (documented); developers only | 1 | AtlasOS L17159 |
| Accessibility feature flags | audio description, blind access and similar; accessibility users | 1 | ReviOS L20908 |
| Administrative shares off | hardening; breaks remote management | 1 | Winaero Tweaker L14614 |
| Anonymous share enumeration restricted | hardening; can break old NAS browsing | 1 | AtlasOS L16951 |
| Automatic color management | Settings switch; only for color-managed displays | 1 | Winhance L5398 |
| Bluetooth advertising off | Bluetooth AllowAdvertising policy; little privacy effect | 1 | O&O ShutUp10++ L3850 |
| Chkdsk countdown at boot | only matters when a boot check is scheduled | 1 | Winaero Tweaker L14559 |
| Chocolatey, Scoop and Oculus packages | users of those tools | 1 | Bulk Crap Uninstaller L23165, L23166, L23167 |
| Classic window metrics | fonts, borders, scrollbars; easy to break the layout | 1 | Winaero Tweaker L14558 |
| Copilot runtime off | AllowCopilotRuntime; unclear scope | 1 | Winhance L4837 |
| Crash on Ctrl+Scroll Lock | for driver debugging | 1 | Winaero Tweaker L14567 |
| Default input method override | multilingual users | 1 | Sophia Script L11855 |
| Default lock screen image | needs an image, cosmetic | 1 | Winaero Tweaker L14577 |
| Developer Mode | developers only; one switch in Settings | 1 | Winhance L4304 |
| Dynamic Lighting foreground control | Dynamic Lighting users only | 1 | Winhance L7927 |
| EFI partition without drive letter | repair after some installs | 1 | AtlasOS L15818 |
| Ease of Access self scan | accessibility users | 1 | AtlasOS L17302 |
| Experimentation off | System AllowExperimentation policy; check edition support | 1 | AtlasOS L16091 |
| Export and import default app associations | DISM export and import exist; useful for reinstalls only | 1 | Sophia Script L12151, L12164 |
| Family Safety task off | only without Family Safety | 1 | Winhance L6303 |
| Fix Windows Installer errors 2502 and 2503 | temp folder permissions; rare repair | 1 | AtlasOS L20027 |
| Focus follows mouse | Accessibility setting; few users | 1 | Winaero Tweaker L14575 |
| HDCP off | for capture devices only | 1 | NVCleanstall L23348 |
| Headset button app off | headset users only | 1 | O&O ShutUp10++ L3891 |
| Hide All Apps in Start | NoStartMenuMorePrograms; kiosk style | 1 | Win11Debloat L2731 |
| Hide Windows Security pages | UILockdown policies; for managed PCs | 1 | AtlasOS L15637, L19931 |
| Hide last user name at sign-in | security policy for shared PCs | 1 | Winaero Tweaker L14581 |
| Hide the Workplace Settings page | managed PCs | 1 | AtlasOS L19076 |
| Input method per app window | multilingual users | 1 | Sophia Script L11018 |
| Insider program hidden | Insider-related policies | 1 | AtlasOS L18113 |
| Intel graphics power plan | Intel iGPU only | 1 | Winhance L6788 |
| Keep the PC awake while a program runs | PowerToys Awake covers it | 1 | Process Lasso L14709 |
| Logitech Download Assistant block | only Logitech users | 1 | WinUtil L782 |
| Move user folders | Windows has it in folder properties; files are not moved | 1 | Sophia Script L11870 |
| NVIDIA HD Audio sleep timer off | fixes cut-off HDMI audio on some monitors and VR headsets | 1 | NVCleanstall L23347 |
| NVMe and storage idle power off | StorageD3InModernStandby, IdlePowerMode; undocumented, laptops lose battery | 1 | AtlasOS L18724 |
| Narrator audio ducking | Narrator users only | 1 | Winhance L7483 |
| Network discovery services off | file sharing users decide | 1 | AtlasOS L19777 |
| Network drives in elevated apps | EnableLinkedConnections; only with mapped drives | 1 | Winaero Tweaker L14615 |
| New Outlook toggle | only Microsoft 365 Outlook users | 1 | WinUtil L1105 |
| New network prompt off | NewNetworkWindowOff; cosmetic | 1 | AtlasOS L17708 |
| No Edge desktop shortcut after updates | EdgeUpdate CreateDesktopShortcut policies; check whether updates honor them | 1 | Sophia Script L12221 |
| Office actions server task off | Office users only | 1 | Winhance L6326 |
| Office add-ins as startup entries | Office users only | 1 | Autoruns L14663 |
| Office first run and sign-in | Office users only | 1 | O&O ShutUp10++ L3834 |
| OneDrive network before sign-in | OneDrive users only | 1 | O&O ShutUp10++ L4140 |
| Pen workspace app suggestions | pen users only | 1 | AtlasOS L16400 |
| Per-game IFEO mitigation fix (osu!) | single game fix | 1 | ReviOS L21722 |
| Performance diagnostics scenario off | WDI scenario; logging only | 1 | AtlasOS L16143 |
| Process automation rules | instance limits, keep running, auto-terminate, watchdog: admin automation, needs a resident process | 1 | Process Lasso L14705, L14706, L14707, L14708, L14710, L14711, L14713 |
| Process creation and PowerShell logging | security auditing for experts | 1 | Sophia Script L12620 |
| Re-register built-in apps | Add-AppxPackage -Register repair for broken inbox apps; rare | 1 | Flyoobe L25911 |
| Refresh the Game Mode known games list | KGL values; undocumented | 1 | ReviOS L22993 |
| Registered owner name | cosmetic | 1 | Winaero Tweaker L14634 |
| Remote Desktop off | off by default; only when it was turned on | 1 | O&O ShutUp10++ L4189 |
| Remote session location | P098; Remote Desktop users only | 1 | O&O ShutUp10++ L3836 |
| Reset local Group Policy | also removes the policies PCO set; use only after Undo all | 1 | Winaero Tweaker L14634 |
| Screenshots folder location | personal preference | 1 | Sophia Script L11883 |
| Sensors off | DisableSensors; breaks auto-rotate and light sensors | 1 | O&O ShutUp10++ L4108 |
| SleepStudy log off | Modern Standby laptops; logging only | 1 | AtlasOS L16007 |
| Sound panel hides disconnected devices | cosmetic | 1 | AtlasOS L16585 |
| Start: no personalized website recommendations | HideRecommendedPersonalizedSites; Enterprise and Education | 1 | AtlasOS L16361 |
| Steam games in the uninstaller | Steam does it itself | 1 | Bulk Crap Uninstaller L23164 |
| System Restore disk use and interval | ReviOS restore configuration; PCO turns protection on with 5 % | 1 | ReviOS L21689 |
| System cooling policy | SYSCOOLPOL power setting; the effect depends on the thermal zones the firmware reports | 1 | Winhance L6865 |
| Touch keyboard typing aids off | touch users | 1 | AtlasOS L16465, L16502, L16522 |
| UAC for the built-in Administrator | only when that account is used | 1 | Winaero Tweaker L14622 |
| USB 3 link power management off | documented power setting; only for USB devices that drop out | 1 | Winhance L6777 |
| USB write protection | for shared or kiosk PCs | 1 | Winaero Tweaker L14573 |
| Uninstall Windows updates | Settings has it; can remove security fixes | 1 | Bulk Crap Uninstaller L23169 |
| Update root certificates from Windows Update | repair for certificate errors on offline images | 1 | ReviOS L22986 |
| Windows Installer in Safe Mode | repair helper for uninstalls in Safe Mode | 1 | ReviOS L21755 |
| Windows Media Player privacy | legacy player users | 1 | AtlasOS L16039 |
| Workplace join prompts off | BlockAADWorkplaceJoin; only with work or school accounts | 1 | Winhance L4228 |

## Not sensible

### Lowers security

| Feature | Reason | Tools | References |
|---|---|---|---|
| Turn off Windows Update | stops security fixes; deferral, notification and driver tweaks cover the usual reasons | 4 | WinUtil L1774; O&O ShutUp10++ L4131; Winaero Tweaker L14574; AtlasOS L14730, L18035 |
| BitLocker decryption | removes disk encryption, security loss, no performance gain worth it | 2 | WinUtil L338; Sparkle L13038 |
| No Mark of the Web on downloads | security feature | 2 | Sophia Script L12684; Winaero Tweaker L14564 |
| PowerShell execution policy | security setting, developers only | 2 | Winhance L4313; AtlasOS L16340 |
| RDP unsigned file warning off | removes a security warning | 2 | WinUtil L515; Sparkle L13356 |
| Security notifications off | Windows Security and maintenance warnings protect the user | 2 | Winhance L7432, L7445; ReviOS L20583 |
| Turn off Defender | same | 2 | Winaero Tweaker L14623; AtlasOS L14728, L19914 |
| UAC level lowered | security feature | 2 | Winhance L4208; Sophia Script L11531 |
| Automatic sign-in (Autologon) | stores the password, security loss | 1 | WinUtil L1448 |
| Biometrics off | removes Windows Hello; security, not privacy | 1 | O&O ShutUp10++ L3859 |
| Block Microsoft accounts | breaks sign-in with a Microsoft account | 1 | AtlasOS L16246 |
| Built-in Administrator, classic UAC dialog, UAC off | security | 1 | Winaero Tweaker L14619, L14620, L14621 |
| CPU affinity and CPU sets per program | anti-cheats block it, Windows and the X3D driver place game threads; needs a resident process | 1 | Process Lasso L14699, L14700 |
| Defender AI host off | security | 1 | RemoveWindowsAI L23403 |
| Defender cloud protection and samples off | security feature | 1 | O&O ShutUp10++ L4146, L4147 |
| Defender real-time protection off | security loss, game folders can be excluded instead | 1 | Sparkle L13140 |
| Edge updates off and legacy Edge prelaunch | security and legacy | 1 | Winaero Tweaker L14630 |
| Elevated shortcut without UAC | bypasses UAC through a scheduled task | 1 | Winaero Tweaker L14634 |
| Hide the Windows Update Settings page | hides security updates from the user | 1 | ReviOS L23031 |
| IFEO Debugger kill for telemetry programs | image hijacks; PCO flags them as suspicious on the Startup page | 1 | ReviOS L22278 |
| Intel TSX on | Microsoft turned TSX off against TAA attacks | 1 | ReviOS L22316, L22636 |
| Lock workstation off | removes Win+L, security loss | 1 | Winhance L4438 |
| Longer update pause | FlightSettingsMaxPauseDays undocumented; long pauses mean no security updates | 1 | Flyoobe L25217 |
| Low disk space warnings off | hides a useful warning; PCO checks free space (F17) | 1 | AtlasOS L17673 |
| Pause updates until 2038 | turns off updates in practice | 1 | ReviOS L23006 |
| Replace Task Manager through IFEO Debugger | an image hijack, the Startup page flags exactly this | 1 | AtlasOS L19645 |
| Security notices and SmartScreen off | Defender tray hidden, SmartScreen off | 1 | ReviOS L22152 |
| Show auto sign-in option | unlocks password-less auto sign-in for Hello accounts | 1 | Winhance L8695 |
| Smart App Control off | security feature; cannot be turned on again without a reset | 1 | Winhance L4287 |
| SmartScreen off | removes the reputation check of downloaded programs, no performance effect | 1 | Sophia Script L12665 |
| SmartScreen or typosquatting checker off | security feature | 1 | O&O ShutUp10++ L4043, L4044, L4045, L4046 |
| Startup app notifications off | the notice about new autostart entries protects the user | 1 | Winhance L7405 |
| System Restore off | removes the safety net | 1 | AtlasOS L18876 |
| Turn off Defender and UAC | security | 1 | ReviOS L22900, L22914 |
| UAC secure desktop off | security | 1 | AtlasOS L17631 |
| UCPD driver off | the driver protects default app choices | 1 | AtlasOS L15363 |
| USB problem notifications off | hides real hardware warnings | 1 | AtlasOS L16532 |
| Unhide password on wake power setting | Settings > Sign-in options has it | 1 | Winaero Tweaker L14570 |

### Windows 10 only or removed from Windows

| Feature | Reason | Tools | References |
|---|---|---|---|
| People and Meet now icons | Windows 10 only | 5 | O&O ShutUp10++ L4176, L4177, L4178; Winhance L9248; AtlasOS L17986; CrapFixer L24459; Flyoobe L25106 |
| Cortana | removed | 3 | O&O ShutUp10++ L4078, L4086; Winhance L4675; Winaero Tweaker L14628 |
| Hide 3D Objects or Music in This PC | Windows 10 only | 3 | Win11Debloat L3396, L3410; Winhance L8524; Bulk Crap Uninstaller L23322 |
| Legacy balloons and Win32 flyouts | Windows 10 flyouts | 3 | Winaero Tweaker L14587, L14588; AtlasOS L19354, L19370, L19386; ReviOS L22776 |
| News and interests | Windows 10 name of widgets | 3 | Winhance L9350; CrapFixer L24439; Flyoobe L25096 |
| Small taskbar icons (Windows 10 value) | TaskbarSmallIcons is ignored on Windows 11 | 3 | Winhance L9379; CrapFixer L24504; Flyoobe L25142 |
| Copilot button and Bing Chat eligibility | obsolete | 2 | O&O ShutUp10++ L4096, L4097; Winhance L9304 |
| Network in Modern Standby off | since Windows 10 2004 Adaptive Connected Standby already keeps the network quiet in sleep on battery unless Remote Desktop or an app set to always run in the background needs it; Disable only breaks those cases | 2 | WinUtil L1166; Win11Debloat L2955 |
| Obsolete Edge policies | same | 2 | CrapFixer L24154, L24163, L24190; Flyoobe L24802, L24811, L24838 |
| Timeline and Windows 10 Start suggestions | Windows 10 only | 2 | O&O ShutUp10++ L3853, L3854; Winhance L4628, L8992 |
| Wi-Fi Sense off | removed in Windows 10 1803 | 2 | Winhance L4249; Sparkle L13575 |
| Chat icon off | Windows 10 and 21H2 only | 1 | Win11Debloat L3088 |
| Classic Folder Options page and search box | hacks for removed Explorer UI | 1 | Winaero Tweaker L14613 |
| Command Prompt in Win+X | Windows 10 value, Windows 11 uses Terminal | 1 | AtlasOS L17851 |
| Copilot availability flags | internal shell values, obsolete | 1 | Winhance L4846, L4855 |
| Edge bar | removed from Edge | 1 | O&O ShutUp10++ L3974, L3975 |
| Edge site safety services | obsolete policy | 1 | O&O ShutUp10++ L4032, L4033 |
| Hide context menu entries (library, give access, share) | Windows 10 context menu only (max build 21999) | 1 | Win11Debloat L3449, L3463, L3481 |
| Internet Explorer add-ons | IE is gone | 1 | Autoruns L14648 |
| Internet Explorer capability | IE is already removed on Windows 11 | 1 | Winhance L9498 |
| Internet Explorer redirection | IE is gone | 1 | O&O ShutUp10++ L4034 |
| Legacy Edge (EdgeHTML) policies | legacy Edge is removed | 1 | O&O ShutUp10++ L4051, L4052, L4053, L4054, L4055, L4056, L4057, L4058, L4060, L4061, L4062 |
| Live tiles | Windows 10 only | 1 | Winaero Tweaker L14590 |
| Obsolete power settings | IE JavaScript timer, slideshow | 1 | Winhance L6641, L6652 |
| Paint 3D context entry | Paint 3D is gone | 1 | AtlasOS L18259 |
| Processor performance increase and decrease policy | legacy non-HWP processors | 1 | Winhance L6887, L6898 |
| Processor performance thresholds | legacy non-HWP processors | 1 | Winhance L6942, L6953 |
| Tablet mode and Chat | Windows 10 and 21H2 only | 1 | AtlasOS L17958, L17967 |
| Wide context menus | Windows 10 only | 1 | Winaero Tweaker L14605 |
| Windows Media Player diagnostics | legacy player | 1 | O&O ShutUp10++ L4187 |

### Needs a program that runs in the background

| Feature | Reason | Tools | References |
|---|---|---|---|
| Re-run removal after each update | background task; PCO lists reset changes after an update instead | 1 | RemoveWindowsAI L23402 |

### Hack that can break Windows or updates

| Feature | Reason | Tools | References |
|---|---|---|---|
| Edge removal | unsupported outside the EEA, breaks WebView and updates | 4 | WinUtil L550; Win11Debloat L2284; Sparkle L13860; ReviOS L20628 |
| Remove protected inbox apps | Paint, Notepad, Calculator, Photos, Snipping Tool, Terminal, Store, App Installer, Edge and shell packages are protected on purpose in appx.json; they are reinstallable, but removing them breaks expected Windows features | 4 | WinUtil L2121, L2122, L2123, L2125, L2127; Win11Debloat L3759, L3766, L3769, L3772, L3773, L3776, L3777, L3778, L3787; Winhance L9445, L9453, L9457, L9458, L9459, L9469, L9476, L9479, L9480, L9487, L9488; AtlasOS L14732 |
| Application compatibility engine off | DisableEngine stops the shims old games need | 2 | AtlasOS L16129; ReviOS L21801 |
| Store results in Start search via ACL deny | denies access to a Store database file; hack, undo fragile | 2 | WinUtil L201; Win11Debloat L2776 |
| Block AI package reinstall with a custom CBS package | servicing hack; Windows Update can fail | 1 | RemoveWindowsAI L23393 |
| BootExecute changes | boot-critical value | 1 | ReviOS L22204 |
| Classic search via TreatAs | CLSID hack | 1 | AtlasOS L17374 |
| Clear Start pins | replaces start2.bin; undocumented binary | 1 | Win11Debloat L2704, L2711 |
| Core parking off | breaks the deliberate parking of the second CCD on dual-CCD Ryzen X3D and core selection on hybrid Intel CPUs; the High performance based Gaming plan already keeps cores unparked on other desktops | 1 | Winhance L6909, L6920 |
| Custom startup sound | the resource is protected on current builds | 1 | Winaero Tweaker L14556 |
| Diagnostic services off | DPS, WdiServiceHost, GraphicsPerfSvc, Ndu, diagsvc: troubleshooters and Task Manager GPU data stop working | 1 | ReviOS L23117, L23134 |
| Dynamic tick off | boot configuration change, higher idle power, no measurement shows a gain in games | 1 | Sparkle L13159 |
| Edit the region policy file | modifies a protected system file | 1 | RemoveWindowsAI L23394 |
| Fault Tolerant Heap off | apps crash instead of being mitigated, no gain | 1 | AtlasOS L15957, L20011 |
| Force close apps at sign-out | AutoEndTasks, data loss risk | 1 | AtlasOS L17809 |
| Microsoft Store off | breaks app updates and Store installs | 1 | AtlasOS L19628 |
| NVENC session limit patch | patches the driver binary | 1 | NVCleanstall L23346 |
| Network connectivity indicator off | breaks internet detection and captive portals | 1 | O&O ShutUp10++ L4194 |
| Old Start menu via feature store override | undocumented feature id (ViVeTool style), breaks with updates | 1 | WinUtil L192 |
| ReadyBoost driver off | rdyboost is a volume filter driver; disabling it without removing the filter entry can stop Windows from booting | 1 | AtlasOS L19848 |
| Registry leftover scan | registry cleaning has no performance benefit and risks breaking programs | 1 | Bulk Crap Uninstaller L23203, L23204, L23205, L23206, L23207, L23208, L23209, L23210, L23211, L23212, L23213, L23214 (+1) |
| Remove Windows components (Defender, CBS packages) | servicing hack, security | 1 | AtlasOS L15210 |
| Remove locked CBS packages and AI files | servicing hack, breaks updates | 1 | RemoveWindowsAI L23397, L23398 |
| Replace Start layout from a template file | start2.bin binary, user-supplied file | 1 | Win11Debloat L2274, L2279 |
| SMB client and server services off | breaks network drives and many apps | 1 | AtlasOS L19698 |
| Set default apps by script | UserChoice hash workaround; Windows protects it (UCPD) | 1 | Sophia Script L12120 |
| Show protected operating system files | easy to delete system files | 1 | Winhance L8263 |
| Take ownership in the context menu | takeown and icacls on any file, easy to break system files | 1 | Winhance L7638 |
| Text input service off | breaks typing in modern apps and IMEs | 1 | ReviOS L22719 |
| WebView2 search via feature override | feature store id | 1 | Winhance L5217 |

### Tool-internal or out of scope

| Feature | Reason | Tools | References |
|---|---|---|---|
| Tool-internal setting | Atlas fixes, new user script, OEM branding | 4 | Winhance L4333; AtlasOS L14733, L14877, L15316, L15333, L15416, L15650, L15672, L15681, L15733, L15748, L15788, L16298 (+8); ReviOS L20135, L20170, L20340, L20406, L21767, L21786; Bulk Crap Uninstaller L23171, L23172 |
| KMS online activation off | activation, out of scope | 2 | O&O ShutUp10++ L4191; AtlasOS L17125 |
| Run another tweak tool | downloads and runs O&O ShutUp10++ | 2 | WinUtil L951; Flyoobe L25744 |
| Classic apps from third parties | Windows 7 games and gadgets from unofficial sources | 1 | Winaero Tweaker L14635 |
| Custom NVIDIA driver package | unattended and clean installs are options of NVIDIA's own installer | 1 | NVCleanstall L23341, L23342, L23343, L23349, L23350 |
| Download and install a cursor theme | downloads third-party assets, cosmetic | 1 | Sophia Script L11056 |
| Hardware requirement bypass | setup-time tool, out of scope (Flyoobe territory) | 1 | ReviOS L22219 |
| Hosts file blocking (Adobe) | editing the hosts file for one vendor; out of scope | 1 | WinUtil L832 |
| Install Open-Shell | third-party shell replacement | 1 | AtlasOS L19459 |
| Install classic Paint and Snipping Tool binaries | copies binaries from other sources | 1 | RemoveWindowsAI L23874, L23884 |
| OOBE pages hidden | setup-time settings (Flyoobe territory) | 1 | ReviOS L22362 |
| Photos Legacy from a downloaded package | unofficial package source | 1 | RemoveWindowsAI L23959 |
| PowerShell 7 as Terminal default | out of scope | 1 | Sparkle L14044 |
| Shell profile install | out of scope (PowerShell profile) | 1 | WinUtil L1675, L1692 |
| Show registry policies in gpedit | tool for Group Policy Editor, out of scope | 1 | Sophia Script L12896 |

### No measurable benefit or a duplicate

| Feature | Reason | Tools | References |
|---|---|---|---|
| MMCSS game profile | same | 4 | Winhance L5273, L5282, L5291, L5324; AtlasOS L15912; CrapFixer L24410; Flyoobe L25058 |
| Win32PrioritySeparation | same | 4 | Winhance L5257; Sparkle L14150; AtlasOS L17020; ReviOS L22441 |
| Automatic maintenance off | maintenance runs TRIM, updates and cleanup | 3 | Winhance L4259; Winaero Tweaker L14563; ReviOS L22261 |
| Automatic updates policy (AUOptions) | turns automatic installs into notify or manual; Microsoft advises it only for compliance | 3 | Winhance L7137; AtlasOS L18423; Flyoobe L25236 |
| Context menu additions | Merge as TrustedInstaller, New script files | 3 | Sophia Script L12799, L12822, L12873; Winaero Tweaker L14601; AtlasOS L18171, L18182, L18192, L18204, L19168, L19184, L19750 |
| Startup delay for apps off | StartupDelayInMSec undocumented; moves load into the first seconds after sign-in | 3 | Winhance L5171; Winaero Tweaker L14572; AtlasOS L17791 |
| svchost split threshold | same | 3 | Winhance L5300; AtlasOS L16978; ReviOS L22530 |
| Accessibility sounds | minor, accessibility | 2 | Winhance L7510, L7519; AtlasOS L17346 |
| Fullscreen optimizations off globally | undocumented GameDVR_FSE values for every game, while FSO problems are game specific | 2 | Winhance L5378; ReviOS L22649 |
| Get me up to date (expedited restarts) | makes restarts more aggressive | 2 | Winhance L7219; Sophia Script L11708 |
| Icon cache size | MaxCachedIcons, no measurable effect | 2 | Winhance L8061; AtlasOS L17493 |
| RSoP logging off | no effect without domain policies | 2 | AtlasOS L16174; ReviOS L21671 |
| Regional formats | Settings page, locale specific; decimal symbols can break apps | 2 | Winhance L8704, L8723, L8744, L8762, L8778, L8794; Sparkle L12917 |
| Shortcut arrow removal | needs a blank icon file, known black square glitches | 2 | Winhance L7545; AtlasOS L19402 |
| netsh TCP global tweaks | heuristics, RSS, timestamps, fast open and window values are Windows defaults or disputed; CTCP replaces the default CUBIC without a measured gain | 2 | Sparkle L13812; AtlasOS L15862, L20074 |
| 8.3 names off | affects only new files, no effect on games; some old installers need short names | 1 | ReviOS L22749 |
| ATI PowerPlay | old Radeon cards | 1 | Winhance L7094 |
| Anonymous SAM enumeration restricted | already the default | 1 | AtlasOS L16892 |
| CPU mitigations off | same | 1 | AtlasOS L14729, L16846, L19963 |
| Compress to in the old context menu | cosmetic, duplicate | 1 | Winhance L7885 |
| Copy Spotlight images | file copy utility | 1 | Winaero Tweaker L14580 |
| DWM composition policy | undocumented | 1 | Winhance L5387 |
| Daylight saving notice off | minor | 1 | Winhance L7423 |
| Device Health Attestation off | only used with device management | 1 | AtlasOS L16082 |
| Diagnostic tracing off | undocumented, no effect | 1 | AtlasOS L17150 |
| Disable devices and network bindings | disables PnP devices and LLDP or LLTD bindings; no measured gain, hard to trace | 1 | AtlasOS L16700 |
| Ethernet as metered (registry ownership) | Settings offers metered per network | 1 | Winaero Tweaker L14616 |
| Explorer start folder OneDrive | trivial and needs OneDrive | 1 | Win11Debloat L3531 |
| Font smoothing off | makes text harder to read, no gain | 1 | Winhance L6505 |
| Global timer resolution requests | restores the old system-wide timer behavior with more power use; games request their own resolution, so no gain | 1 | AtlasOS L18892 |
| Handwriting data sharing off | the current ADMX_ICM Policy CSP lists no handwriting policy, so the effect of PreventHandwritingDataSharing and PreventHandwritingErrorReports on Windows 11 is unconfirmed; pen input only | 1 | O&O ShutUp10++ L3843, L3844 |
| Hardware overlays off, MPO minimum FPS | undocumented; MPO is handled through the NVIDIA documented value | 1 | Winhance L5418, L5429 |
| IPv6 off | no measured gain, IPv6 is part of Windows networking; network.preferIpv4 covers the real problem case (broken or slow IPv6 routes) | 1 | WinUtil L918 |
| Legacy NVIDIA sharpening | undocumented driver value | 1 | Winhance L5369 |
| Location request notice off | privacy notice | 1 | Winhance L7414 |
| Multimedia power settings | minor | 1 | Winhance L6973, L6984, L6995 |
| NTFS memory usage | server setting, more paged pool, no gain on a desktop | 1 | ReviOS L22762 |
| Null session access restricted | already the default | 1 | AtlasOS L16936 |
| Paging executive and page combining off | no measured gain, more memory use | 1 | AtlasOS L16968 |
| Per-app microphone consent for Copilot | covered by the microphone policy | 1 | Winhance L4885 |
| Prefetch folder cleanup | Windows rebuilds it; the next app starts get slower | 1 | Sparkle L14293 |
| Prefetch off | no gain on SSDs, SysMain covers it | 1 | Winhance L5588 |
| ProBalance | Windows already favors the foreground program; no measured gain on current multi-core CPUs; needs a resident process | 1 | Process Lasso L14692 |
| Process tuning rules | GPU priority, foreground and startup boost, timer resolution: no measured gain in games, resident process needed | 1 | Process Lasso L14702, L14703, L14704, L14712 |
| Processor C-state tuning | changes idle states, more heat, no measured gain | 1 | ReviOS L22574 |
| Processor idle states off | more heat and power, no measured gain | 1 | AtlasOS L18457, L18476 |
| RDP port and IP routing | server settings, no use on a gaming PC | 1 | Winaero Tweaker L14617, L14618 |
| Repair commands in the context menu | SFC, DISM and chkdsk are on the Health and Tools pages | 1 | Winhance L7732, L7766, L7783, L7820 |
| Restart at the scheduled time | only with scheduled installs; forces restarts | 1 | Flyoobe L25252 |
| Run with priority context menu | its Realtime entry can starve system threads (input, disk cache); start priorities exist | 1 | AtlasOS L19123 |
| SMT off per process | BIOS setting for a few X3D or old games, no general gain | 1 | Process Lasso L14715 |
| Scheduled cleanup tasks | Storage Sense does the temp cleanup on a schedule; PCO has no background part by design | 1 | Sophia Script L12409, L12469, L12515 |
| Search entire file system | makes Explorer search slower | 1 | Winhance L5208 |
| ServicesPipeTimeout | no performance effect | 1 | Winhance L5562 |
| Show or hide extensions in the context menu | duplicate of explorer.fileExtensions | 1 | Winhance L7688 |
| Slow animations with Shift | debugging gimmick | 1 | Winaero Tweaker L14555 |
| SmartTrim and standby list clearing | no measured gain; Windows manages the standby list | 1 | Process Lasso L14714 |
| Taskbar position and tray rows | needs ExplorerPatcher or StartAllBack on Windows 11 | 1 | Winaero Tweaker L14600 |
| Teredo off | no latency gain; Xbox party chat and Xbox Live multiplayer use Teredo behind NAT, the Xbox app then reports "Teredo is unable to qualify" | 1 | WinUtil L894 |
| Tooltip color | Atlas cosmetic | 1 | AtlasOS L17281 |
| USB hub selective suspend timeout | minor; selective suspend itself is covered | 1 | Winhance L6755 |
| Updates over metered connections | default is already off | 1 | Winhance L7261 |

## Already in Tunero

Features of the tools that Tunero covers, with the tweak, page or check that does it.

| Feature | In Tunero | Tools | References |
|---|---|---|---|
| Remove inbox apps | Remove apps page (126 reviewed apps with Xbox guard and Store links) | 11 | WinUtil L2104, L2105, L2106, L2108, L2109, L2110 (+20); Win11Debloat L2254, L3674, L3675, L3676, L3677, L3678 (+91); Winhance L9435, L9436, L9437, L9438, L9439, L9440 (+31); Sophia Script L12328; Sparkle L12959; AtlasOS L15109; ReviOS L20728, L20813, L21602; Bulk Crap Uninstaller L23163; RemoveWindowsAI L23395; CrapFixer L24587, L24588, L24589, L24590, L24591, L24592 (+34); Flyoobe L25262, L25263, L25264, L25265, L25266, L25267 (+35) |
| OneDrive uninstall | Remove apps page card | 8 | WinUtil L590; Win11Debloat L3764; Winhance L9489; Sophia Script L11394; AtlasOS L15210; ReviOS L20628; CrapFixer L24616; Flyoobe L25293 |
| Tips and suggestions off | privacy.suggestionsOff, focus.finishSetupOff, explorer.syncProviderNotificationsOff, privacy.cloudContentOff, focus.lockScreenTipsOff | 8 | Win11Debloat L2319; O&O ShutUp10++ L3831, L3855, L3856, L4137, L4186; Winhance L4383, L7354, L7365, L7374, L7385; Sophia Script L10255, L10280, L10306; AtlasOS L15517; ReviOS L21126, L21826; CrapFixer L24010, L24019, L24028, L24047, L24058, L24067 (+2); Flyoobe L24658, L24667, L24676, L24695, L24706, L24715 (+2) |
| Game Bar captures off | gpu.gameDvrOff, and gpu.gameDvrPolicyOff by policy on Pro and higher | 7 | Win11Debloat L2667; O&O ShutUp10++ L4199; Winhance L5533; Sophia Script L12349; AtlasOS L15980, L18517; CrapFixer L24246; Flyoobe L24894 |
| Location off | privacy.locationOff, privacy.locationServiceOff, privacy.findMyDeviceOff | 7 | WinUtil L221; Win11Debloat L2362; O&O ShutUp10++ L3883, L3884, L4106, L4107, L4109; Winhance L4685; AtlasOS L18668; CrapFixer L24305; Flyoobe L24953 |
| Telemetry off | privacy.telemetryOff (AllowTelemetry, DiagTrack, seven telemetry tasks) plus the inking, tailored experience, feedback and app compatibility tweaks | 7 | WinUtil L270; Win11Debloat L2290; O&O ShutUp10++ L3943, L4114, L4115, L4116, L4117, L4118 (+1); Winhance L4553, L4565, L4582, L4594, L4607, L4618 (+7); Sophia Script L9949, L9972, L10005, L10040, L10068; Sparkle L13451; AtlasOS L15611, L17172, L17196 |
| Windows features | Tools page: 22 optional features and capabilities (Hyper-V, WSL, Sandbox, .NET 3.5, legacy media, NFS client, WordPad, Steps Recorder, OpenSSH client and more) | 7 | WinUtil L1333, L1340, L1347, L1354, L1361, L1416 (+1); Win11Debloat L3656, L3663; Winhance L9499, L9501, L9502, L9503, L9506; Sophia Script L11615, L11635, L12314, L12713; Bulk Crap Uninstaller L23168; RemoveWindowsAI L23396; Flyoobe L25893 |
| App installer | Apps page: 26 reviewed winget apps (launchers, Discord, OBS, monitoring tools, browsers, runtimes) and Update all | 6 | WinUtil L1798, L1801, L1821, L1829, L1916, L1919 (+17); Winhance L9622, L9625, L9626, L9650, L9671, L9739 (+11); Sparkle L14328, L14330, L14348, L14396, L14397, L14398 (+9); AtlasOS L14734, L18317; ReviOS L20554; Flyoobe L25319, L25323, L25345, L25465, L25467, L25469 (+14) |
| Classic context menu | explorer.classicContextMenu | 6 | WinUtil L863; Win11Debloat L2875; Winhance L7629; Winaero Tweaker L14604; AtlasOS L19207; ReviOS L20866 |
| Edge debloat policies | 21 edge.* tweaks (diagnostic data, shopping, sidebar, first run, new tab, promotions, sign-in, AI, suggestions) | 6 | WinUtil L525; Win11Debloat L2402; O&O ShutUp10++ L3954, L3955, L3956, L3957, L3958, L3959 (+62); Winhance L4985; CrapFixer L24172, L24181, L24199, L24208, L24217, L24226 (+1); Flyoobe L24820, L24829, L24847, L24856, L24865, L24874 (+1) |
| Background apps off | background.backgroundAppsOff | 5 | WinUtil L942; O&O ShutUp10++ L3936; Winhance L5180; Sparkle L12995; AtlasOS L15947, L18439 |
| Copilot off | app removal; TurnOffWindowsCopilot is legacy | 5 | Win11Debloat L2528; O&O ShutUp10++ L4092, L4093; Winhance L4746; CrapFixer L24107; Flyoobe L24755 |
| End task in taskbar menu | explorer.endTask | 5 | WinUtil L379; Win11Debloat L3102; Winhance L9388; Sophia Script L10829; AtlasOS L17977 |
| Explorer basics | This PC icon, check boxes, hidden files, extensions | 5 | Winhance L8099, L8108; Sophia Script L10329, L10349, L10368, L10387, L10425, L10444 (+2); AtlasOS L17610, L17620; ReviOS L21023; Flyoobe L25879 |
| Mouse acceleration off | input.mouseAccelOff | 5 | WinUtil L1136; Win11Debloat L2888; Winhance L5142; ReviOS L20908; Bulk Crap Uninstaller L23268 |
| Services to Manual | lfsvc, RetailDemo, wisvc, PhoneSvc | 5 | WinUtil L238; Winhance L5650, L5668, L5702, L5719, L5736, L5753 (+9); Sparkle L14114; AtlasOS L15363, L19730; ReviOS L20502 |
| Sticky Keys shortcut off | input.accessibilityShortcutsOff | 5 | WinUtil L1248; Win11Debloat L2903; Winhance L6547, L6556, L6565; AtlasOS L17312; Bulk Crap Uninstaller L23238 |
| Widgets off | background.widgetsOff | 5 | WinUtil L173; Win11Debloat L3081; O&O ShutUp10++ L4174; Winhance L9340; CrapFixer L24449 |
| Consumer features off | privacy.consumerFeaturesOff | 4 | WinUtil L261; Sophia Script L10229; Winaero Tweaker L14631; Bulk Crap Uninstaller L23245 |
| Dark mode | personalize.darkMode | 4 | WinUtil L1011; Win11Debloat L2441; Winaero Tweaker L14554; ReviOS L20170 |
| Delivery Optimization P2P off | network.deliveryOptimizationP2POff | 4 | WinUtil L329; Win11Debloat L3629; Winhance L7182; AtlasOS L18072, L18501 |
| Explorer start folder | office.launchToThisPc (This PC); Downloads is a Could row | 4 | Win11Debloat L3494, L3505; Winhance L7945; Winaero Tweaker L14612; AtlasOS L17564 |
| Gaming and privacy basics | power throttling, visual effects, activity history, app launch tracking, telemetry | 4 | Sparkle L13188, L13207, L13241, L13260, L13310, L13333; AtlasOS L14731, L16409, L16416, L16425; CrapFixer L24257, L24265, L24275, L24284, L24293; Flyoobe L24905, L24913, L24923, L24932, L24941 |
| Hibernation off | power.hibernateOff | 4 | WinUtil L148; Winhance L6696; CrapFixer L24373; Flyoobe L25021 |
| Network throttling off | network.throttlingIndex | 4 | Winhance L5440; ReviOS L22347; CrapFixer L24392; Flyoobe L25040 |
| Online speech recognition off | privacy.onlineSpeechOff | 4 | O&O ShutUp10++ L4079, L4080; Winhance L4522; CrapFixer L24332; Flyoobe L24980 |
| Open with: no Store | explorer.openWithPromptsOff | 4 | O&O ShutUp10++ L3863; Sophia Script L12844; Winaero Tweaker L14562, L14569; AtlasOS L17555 |
| Power plan choice | Balanced, Tunero Gaming (copy of High performance) and the Ultimate Performance plan; the plan already uses 100 % minimum processor state on mains power | 4 | Winhance L6614; Sophia Script L11794; Process Lasso L14694; Flyoobe L25776 |
| Restore point before changes | restore point once per session | 4 | WinUtil L358; Win11Debloat L2269; Sophia Script L9937; Flyoobe L25766 |
| Spotlight off | privacy.spotlightOff | 4 | O&O ShutUp10++ L4190; Winhance L4447; CrapFixer L24352; Flyoobe L25000 |
| Taskbar basics | alignment, widgets, search, highlights, Task View, seconds | 4 | Winhance L9188, L9197, L9214; Sophia Script L10607, L10626, L10655, L10695, L10724, L10751; CrapFixer L24468, L24477, L24486, L24495; Flyoobe L25115, L25124, L25133 |
| VBS and memory integrity off | security.vbsOff (Expert) | 4 | Winhance L5509, L5520; Sparkle L13115; AtlasOS L16676, L19872; ReviOS L22188, L22945, L22970 |
| Verbose status messages | system.verboseStatus | 4 | WinUtil L1096; Winaero Tweaker L14586; AtlasOS L17800; CrapFixer L24419 |
| Visual effects best performance | visual.bestPerformance | 4 | WinUtil L650; Win11Debloat L2468; Winhance L6333, L6351, L6362, L6373, L6384, L6395 (+9); AtlasOS L16627 |
| Advertising ID off | privacy.advertisingIdOff | 3 | O&O ShutUp10++ L3847, L3848; CrapFixer L24038; Flyoobe L24686 |
| App notifications off | focus.toastsOff | 3 | Win11Debloat L2349; O&O ShutUp10++ L3860; Winhance L7304 |
| App permissions | contacts, calendar, phone, call history, email, tasks, messages | 3 | O&O ShutUp10++ L3878, L3879, L3881, L3882, L3885, L3886 (+28); Winhance L4698, L4707, L4716, L4725; AtlasOS L16028 |
| Device companion apps off | privacy.deviceMetadataOff | 3 | WinUtil L397; Win11Debloat L3642; O&O ShutUp10++ L4126, L4127 |
| Driver updates off | updates.driversExcluded | 3 | Winhance L7270; Winaero Tweaker L14565; Flyoobe L25244 |
| Fast startup off | power.fastStartupOff | 3 | Win11Debloat L2929; Winhance L6733; AtlasOS L16867 |
| Long paths | system.longPaths | 3 | WinUtil L1311; Winhance L8686; AtlasOS L17484 |
| MPO off | gpu.mpoOff | 3 | WinUtil L1126; Winhance L5407; NVCleanstall L23345 |
| Notepad AI off | ai.notepadOff | 3 | Win11Debloat L2653; O&O ShutUp10++ L3828; RemoveWindowsAI L23400 |
| Paint AI off | ai.paintOff | 3 | Win11Debloat L2636; O&O ShutUp10++ L4095, L4100, L4101; Winhance L4895, L4904, L4913 |
| Recall off | ai.recallOff, background.aiOff | 3 | Win11Debloat L2545; O&O ShutUp10++ L4094, L4098, L4099; Winhance L4756, L4765, L4774 |
| Show file extensions | explorer.fileExtensions | 3 | WinUtil L1044; Win11Debloat L3290; Winhance L8128 |
| Start lists | personalize.startRecentlyAddedHidden, startMostUsedHidden | 3 | Winhance L8965, L8974, L8983; Sophia Script L11195, L11274; AtlasOS L16361 |
| Storage Sense | storage.storageSenseOn (undo turns it off) | 3 | WinUtil L705; Win11Debloat L2916; Winhance L5198 |
| Store auto update off | updates.storeAutoUpdateOff | 3 | O&O ShutUp10++ L4128; Winhance L7293; Winaero Tweaker L14627 |
| Task View button hidden | personalize.taskViewHidden | 3 | WinUtil L1292; Win11Debloat L3068; Winhance L9295 |
| Taskbar alignment | personalize.taskbarLeft | 3 | WinUtil L1257; Win11Debloat L3055; Sparkle L12940 |
| Taskbar search mode | SearchOnTaskbarMode policy (hidden, icon); label and box are the default choices | 3 | WinUtil L1283; Win11Debloat L3128, L3141, L3154, L3167; O&O ShutUp10++ L4175 |
| Web search off | privacy.searchConnectedOff | 3 | O&O ShutUp10++ L4059, L4081, L4082, L4083, L4169; Winaero Tweaker L14591; ReviOS L21155 |
| Windows AI off | Recall, Notepad, Paint, Copilot app removal; AI settings page and WSAIFabricSvc see separate items | 3 | WinUtil L714; Sophia Script L12275; RemoveWindowsAI L23392 |
| Activity history off | privacy.activityHistoryOff | 2 | WinUtil L137; O&O ShutUp10++ L3868, L3869, L3870 |
| Aero Shake off | personalize.titleBarShakeOff | 2 | Sophia Script L11029; Winaero Tweaker L14560 |
| Alt+Tab tabs | focus.altTabTabsOff (open windows only); 3, 5, 20 tabs are a Settings choice | 2 | Win11Debloat L3010, L3021, L3032, L3043; Winhance L5248 |
| App launch tracking off | privacy.appLaunchTrackingOff | 2 | O&O ShutUp10++ L3880; AtlasOS L16202 |
| Automatic device encryption off | security.deviceEncryptionPrevented (Expert) | 2 | Win11Debloat L2942; Winhance L4240 |
| Bing in Start search off | privacy.webSearchOff, privacy.searchConnectedOff | 2 | WinUtil L1193; Win11Debloat L2759 |
| Brave policies | brave.*; missing TorDisabled, MetricsReportingEnabled, SafeBrowsingExtendedReportingEnabled, UrlKeyedAnonymizedDataCollectionEnabled (see Brave: more policies) | 2 | WinUtil L495; Win11Debloat L2843 |
| Broken shortcut search off | explorer.shortcutSearchOff | 2 | AtlasOS L17447; ReviOS L21210 |
| CEIP off | privacy.ceipOff | 2 | O&O ShutUp10++ L3851; AtlasOS L17140 |
| Classic F8 boot menu | expert.legacyBootMenu | 2 | WinUtil L1394, L1405; AtlasOS L16314 |
| Cleanup categories | recycle bin, update cache, thumbnails, crash dumps | 2 | Sparkle L14294, L14295, L14296, L14297; Bulk Crap Uninstaller L23200 |
| Cloud content and dumps limited | privacy.cloudContentOff, privacy.diagnosticLogsLimited, privacy.consumerFeaturesOff | 2 | O&O ShutUp10++ L3835; Winhance L4958 |
| Cloud content in search off | privacy.cloudSearchOff | 2 | O&O ShutUp10++ L3830, L4085; Winhance L4657, L4666 |
| Communications ducking off | personalize.communicationsDuckingOff | 2 | Winhance L7465; ReviOS L21706 |
| DNS presets | Cloudflare, Google, Quad9, OpenDNS, AdGuard, automatic, benchmark, fastest | 2 | WinUtil L961; Winhance L5461 |
| Edge AI off | edge.aiOff | 2 | Win11Debloat L2588; Winhance L5012, L5030, L5039, L5048 |
| Explorer compact view | explorer.compactView | 2 | Winhance L8043; AtlasOS L19272 |
| Feature updates deferral | updates.featureUpdatesDeferred | 2 | O&O ShutUp10++ L4125; AtlasOS L19038 |
| Find my device off | privacy.findMyDeviceOff | 2 | Win11Debloat L2375; O&O ShutUp10++ L3829 |
| Game Mode | gpu.gameMode | 2 | WinUtil L1301; Winhance L5133 |
| HAGS | gpu.hags | 2 | Winhance L5333; Sophia Script L12389 |
| Hibernate in power menu | personalize.hibernateInPowerMenu | 2 | Winhance L6744; AtlasOS L18650 |
| Inking and typing personalization off | privacy.inkingTypingOff (TIPC is covered by telemetry level) | 2 | O&O ShutUp10++ L3849; Winhance L4940 |
| Item check boxes | explorer.itemCheckboxes | 2 | Winhance L8346; AtlasOS L17398 |
| Lock screen tips off | focus.lockScreenTipsOff | 2 | Win11Debloat L2388; Winhance L4458 |
| Maps traffic off | privacy.mapsTrafficOff | 2 | O&O ShutUp10++ L4192, L4193; Winhance L6289 |
| NTFS last access off | storage.lastAccessOff | 2 | AtlasOS L17003; ReviOS L22736 |
| Notification center off | focus.notificationCenterOff | 2 | WinUtil L822; Winaero Tweaker L14589 |
| Phone link off | privacy.phoneLinkOff, privacy.crossDeviceOff | 2 | O&O ShutUp10++ L4160, L4161, L4163; AtlasOS L17691, L18701 |
| Privacy basics | language list, advertising ID, welcome experience, tips, suggested content | 2 | Winhance L4470, L4482, L4491, L4500; Sophia Script L10115, L10134, L10161, L10180, L10206 |
| Program Compatibility Assistant off | background.pcaOff | 2 | AtlasOS L16129; ReviOS L21801 |
| Recall, search suggestions, Edge basics | ai.recallOff, privacy.webSearchOff, edge.signInOff, edge.newTabQuiet | 2 | CrapFixer L24116, L24125, L24136, L24145; Flyoobe L24764, L24773, L24784, L24793 |
| Recent files off | privacy.recentFilesOff | 2 | O&O ShutUp10++ L4138; AtlasOS L17511 |
| Registry backup | system.registryBackup | 2 | WinUtil L1378; Sophia Script L12249 |
| Remote Assistance off | security.remoteAssistanceOff | 2 | O&O ShutUp10++ L4188; Winhance L4278 |
| Reserved storage off | storage.reservedStorageOff | 2 | WinUtil L685; Sophia Script L11940 |
| Search highlights off | privacy.searchHighlightsOff | 2 | O&O ShutUp10++ L4087; Winhance L4648 |
| Settings sync off | privacy.settingsSyncOff | 2 | O&O ShutUp10++ L4067; AtlasOS L17080 |
| Show hidden files | explorer.hiddenFiles | 2 | WinUtil L1070; Win11Debloat L3303 |
| Snap layouts off | personalize.snapLayoutsOff | 2 | Win11Debloat L2996; AtlasOS L19441 |
| Start most used and snap layouts | personalize.startMostUsedHidden, personalize.snapLayoutsOff | 2 | CrapFixer L24551, L24559; Flyoobe L25179, L25187 |
| Start privacy | recent files, recommendations, account notifications, Bing | 2 | Winhance L9003, L9014, L9023, L9032; Sophia Script L11355, L11374 |
| Startup entries | the Security tray icon is a Run entry on the Startup page | 2 | Winaero Tweaker L14625; AtlasOS L19947 |
| Sync provider ads off | explorer.syncProviderNotificationsOff | 2 | O&O ShutUp10++ L4139; Winhance L8337 |
| SysMain off | memory.sysmainOff | 2 | Winhance L5571; AtlasOS L19848 |
| Taskbar combine buttons | personalize.taskbarNeverCombine | 2 | Win11Debloat L3178, L3189, L3200, L3213, L3226, L3239; Sophia Script L10789 |
| Temp files cleanup | Cleanup page | 2 | WinUtil L419; Sparkle L14292 |
| Transparency off | visual.transparencyOff | 2 | Win11Debloat L2455; CrapFixer L24577 |
| Ultimate Performance plan | power.ultimatePlan | 2 | WinUtil L1321, L1326; ReviOS L22537 |
| Update notifications reduced | focus.updateNotificationsReduced | 2 | Winhance L7240; AtlasOS L18914 |
| Windows Error Reporting off | privacy.errorReportingOff | 2 | O&O ShutUp10++ L3858; Winhance L4268 |
| .NET Desktop Runtimes | apps.json | 1 | Sophia Script L12205 |
| Accent color on title bars | personalize.accentTitleBars | 1 | Winaero Tweaker L14551 |
| Activity history and app launch tracking | privacy.activityHistoryOff, privacy.appLaunchTrackingOff | 1 | AtlasOS L16058, L16067 |
| Activity history and search | privacy.activityHistoryOff, search policies | 1 | AtlasOS L16255, L16265 |
| Advertising ID and sync ads | privacy.advertisingIdOff, explorer.syncProviderNotificationsOff | 1 | AtlasOS L17031, L17041 |
| Alt+Tab, AutoPlay, Aero Shake | focus.altTabTabsOff, security.autoPlayOff, personalize.titleBarShakeOff | 1 | AtlasOS L17642, L17651, L17664 |
| App archiving off | storage.appArchivingOff | 1 | AtlasOS L18860 |
| Application compatibility telemetry off | privacy.appCompatTelemetryOff | 1 | O&O ShutUp10++ L3845 |
| AutoPlay off | security.autoPlayOff | 1 | Winhance L8812 |
| Autoruns locations | Startup page lists all of them (per-user shell extensions open in docs/TODO.md) | 1 | Autoruns L14646, L14647, L14649, L14650, L14651, L14652 (+11) |
| Bing and classic menu | privacy.webSearchOff, explorer.classicContextMenu | 1 | Flyoobe L25153, L25162 |
| Bing, dark mode, classic menu | privacy.webSearchOff, personalize.darkMode, explorer.classicContextMenu | 1 | CrapFixer L24515, L24524, L24534 |
| CEIP and cloud content off | privacy.ceipOff, privacy.cloudContentOff, Spotlight | 1 | ReviOS L21895, L21917 |
| Change log and rule import | Changes page log; export and import see the separate item | 1 | Process Lasso L14716 |
| Chrome AI off | chrome.aiOff | 1 | Win11Debloat L2608 |
| Classic menu and input personalization | explorer.classicContextMenu, privacy.inkingTypingOff | 1 | ReviOS L22806, L22823 |
| Clipboard history off | privacy.clipboardHistoryOff | 1 | O&O ShutUp10++ L3871, L3872 |
| Clipboard sync off | privacy.cloudClipboardOff | 1 | O&O ShutUp10++ L3873 |
| Clock seconds and Ultimate Performance plan | personalize.clockSeconds, power.ultimatePlan | 1 | Sparkle L14169, L14196 |
| Clock seconds and flashing | personalize.clockSeconds, focus.taskbarFlashingOff | 1 | Winaero Tweaker L14595, L14596 |
| Cloud content and Copilot | privacy.cloudContentOff, Copilot app removal | 1 | AtlasOS L17912, L17921 |
| Consumer features and Copilot | privacy.consumerFeaturesOff, Copilot app removal | 1 | Sparkle L13051, L13096 |
| Copilot and Recall off | app removal, background.aiOff | 1 | AtlasOS L18384, L18407 |
| DNS over HTTPS | network.dohAutoUpgrade | 1 | Sophia Script L12732 |
| DRM online off | privacy.drmOnlineOff | 1 | O&O ShutUp10++ L3944 |
| Dark mode and first logon animation | personalize.darkMode, personalize.firstLogonAnimationOff | 1 | Sophia Script L10890, L10909, L10928 |
| Dark mode and transparency | personalize.darkMode, visual.transparencyOff | 1 | Winhance L9398, L9415 |
| Debloat basics | gaming apps, Bing apps and search, OneDrive, classic context menu | 1 | Sparkle L13888, L13906, L13963, L14021 |
| Default printer stays | personalize.defaultPrinterManual | 1 | Winhance L7936 |
| Defender PUA protection | security.puaProtection | 1 | Winaero Tweaker L14624 |
| Defender options | network protection, PUA, Defender sandbox | 1 | Sophia Script L12563, L12582, L12601 |
| Delayed ACK off | network.nagleOff; TCPNoDelay under the interfaces is not described by Microsoft, programs turn off Nagle per socket | 1 | Winhance L5449 |
| Delivery Optimization and default printer | network.deliveryOptimizationP2POff, personalize.defaultPrinterManual | 1 | Sophia Script L11564, L11590 |
| Disk Cleanup and component cleanup | Cleanup page, Superseded Windows components | 1 | WinUtil L406 |
| Driver updates | Drivers card links to the vendor; PCO installs no drivers on purpose | 1 | AtlasOS L18367 |
| Drivers, hibernation, fast startup | updates.driversExcluded, power.hibernateOff, power.fastStartupOff | 1 | ReviOS L23044, L23071, L23088 |
| Dynamic Lighting off | personalize.dynamicLightingOff | 1 | Winhance L7918 |
| Energy performance preference | set by the Gaming plan (copy of High performance) | 1 | Winhance L6931 |
| Error reporting and message sync | privacy.errorReportingOff, privacy.messageSyncOff | 1 | ReviOS L21961 |
| Feedback requests off | privacy.feedbackNotificationsOff | 1 | O&O ShutUp10++ L4183, L4184 |
| Feedback, Spotlight, ducking | privacy.feedbackNotificationsOff, privacy.spotlightOff, personalize.communicationsDuckingOff | 1 | AtlasOS L16551, L16562, L16576 |
| Finish setup and message sync | focus.finishSetupOff, privacy.messageSyncOff | 1 | AtlasOS L17105, L17114 |
| Finish setup screen off | focus.finishSetupOff | 1 | O&O ShutUp10++ L3857 |
| First sign-in animation off | personalize.firstLogonAnimationOff | 1 | ReviOS L22330 |
| GPU preference | F4 check and fix.gpuPreference | 1 | Winhance L7105 |
| Gaming and taskbar basics | dark mode, End task, Game Mode, HAGS, windowed optimizations, Task View | 1 | Sparkle L13596, L13623, L13656, L13681, L13720, L13758 |
| Installed programs list | Programs card (uninstall keys) | 1 | Bulk Crap Uninstaller L23162 |
| LLMNR off | security.llmnrOff | 1 | AtlasOS L15869 |
| LSA protection | security.lsaProtection | 1 | Sophia Script L12771 |
| Language list and error reporting | privacy.languageListOff, privacy.errorReportingOff | 1 | AtlasOS L16211, L16220 |
| Language list for websites off | privacy.languageListOff | 1 | O&O ShutUp10++ L3861 |
| Lock screen camera and online speech | privacy.lockScreenCameraOff, privacy.onlineSpeechOff | 1 | AtlasOS L16100, L16109 |
| Lock screen camera off | privacy.lockScreenCameraOff | 1 | O&O ShutUp10++ L3846 |
| Lock screen notifications off | focus.lockScreenToastsOff | 1 | Winhance L7322 |
| Log file | Settings > Log file | 1 | Sophia Script L9934 |
| MSI mode for the graphics card | device.msiMode (Expert) | 1 | NVCleanstall L23344 |
| Malicious Software Removal Tool reports off | privacy.malwareReportOff | 1 | AtlasOS L18134 |
| Maximum processor state | power.turboRestore, quiet profile | 1 | Winhance L6854 |
| Minimum processor state 100 | the Gaming plan (copy of High performance) uses 100 % on mains power | 1 | Winhance L6843 |
| NVIDIA global settings | NVAPI settings (low latency, shader cache, texture filtering, power) and the F7 reset of limiting values | 1 | Sparkle L13853 |
| Network adapter power off not allowed | network.nicAllowPowerOffOff | 1 | Sophia Script L11820 |
| Network reset | Winsock and TCP/IP reset quick fixes | 1 | WinUtil L1515 |
| No automatic sign-in after restarts | security.autoRestartSignOnOff | 1 | Sophia Script L10088 |
| No projection to this PC | security.projectionToPcOff | 1 | O&O ShutUp10++ L3946 |
| Notification settings | notification center, sounds (undo of Atlas defaults) | 1 | AtlasOS L14742, L15762 |
| Notification sounds off | focus.notificationSoundsOff | 1 | Winhance L7313 |
| OneDrive folder backup off | privacy.oneDriveFolderBackupOff | 1 | Winhance L4734 |
| OneDrive sync off | background.oneDriveSyncOff | 1 | O&O ShutUp10++ L4141 |
| PCIe ASPM off | power.pcieAspmOff | 1 | Winhance L6832 |
| Password reveal and telemetry | security.passwordRevealOff, privacy.telemetryOff | 1 | Winaero Tweaker L14632, L14633 |
| Password reveal button off | security.passwordRevealOff | 1 | O&O ShutUp10++ L3941 |
| Peek and widgets | visual.bestPerformance, background.widgetsOff | 1 | AtlasOS L17936, L17945 |
| Performance counter rebuild | quick fix | 1 | AtlasOS L15803 |
| Power mode | quiet.powerModeEfficiency, fix.powerMode | 1 | Winhance L7083 |
| Power saving off | power.throttlingOff, network.nicAllowPowerOffOff | 1 | AtlasOS L18724 |
| Power throttling off | power.throttlingOff | 1 | Winhance L6964 |
| Prefer IPv4 | network.preferIpv4 | 1 | WinUtil L885 |
| Print Screen and Settings tips | input.printScreenSnippingOff, privacy.onlineTipsOff | 1 | AtlasOS L16446, L16455 |
| Print Screen opens Snipping Tool | input.printScreenSnippingOff | 1 | Sophia Script L10999 |
| Privacy experience and Recall | privacy.privacyExperienceOff, Recall | 1 | AtlasOS L16158, L16167 |
| Privacy settings page at sign-in off | privacy.privacyExperienceOff | 1 | ReviOS L22362 |
| Processor boost mode | PERFBOOSTMODE in turbo, battery and quiet tweaks | 1 | Winhance L6876 |
| Program start priorities | Tools page, IFEO PerfOptions | 1 | Process Lasso L14697, L14698 |
| Recall and taskbar search | ai.recallOff, taskbar search policy | 1 | Sparkle L13390, L13414 |
| Recent items, verbose status, visual effects | privacy.recentFilesOff, system.verboseStatus, visual.bestPerformance | 1 | AtlasOS L19471, L19507, L19523 |
| Recovery environment on | quick fix | 1 | AtlasOS L15781 |
| Remote Assistance and SMB throttling | security.remoteAssistanceOff, network.smbThrottlingOff | 1 | AtlasOS L16907, L16921 |
| Remove OEM apps | Remove apps page OEM group | 1 | Win11Debloat L2264 |
| Remove Xbox apps | Remove apps page with Xbox guard | 1 | Win11Debloat L2259 |
| Restart Explorer | quick fix | 1 | AtlasOS L19094 |
| Restart Explorer and registry backup | quick fix, system.registryBackup | 1 | Winaero Tweaker L14634 |
| SFC and DISM | Health page | 1 | WinUtil L1525 |
| Search history off | privacy.searchHistoryOff | 1 | Winhance L4639 |
| Search indexing to Manual | Services page offers Manual for WSearch with a warning; a tweak is not needed: search gets slow and the indexer already backs off under load | 1 | Winhance L5599 |
| Search, widgets, Spotlight | search policies, background.widgetsOff, privacy.spotlightOff | 1 | AtlasOS L18934, L18979, L19000 |
| Settings Home ads off | privacy.cloudContentOff | 1 | Win11Debloat L2427 |
| Sign-in screen without blur | personalize.logonBlurOff | 1 | WinUtil L1202 |
| Snap suggestions off | personalize.snapAssistOff | 1 | Win11Debloat L2983 |
| Spotlight and lock screen | privacy.malwareReportOff, privacy.spotlightOff, focus.lockScreenTipsOff, focus.lockScreenToastsOff | 1 | O&O ShutUp10++ L4148, L4153, L4154, L4155 |
| Start recommendations and classic menu | privacy.suggestionsOff, explorer.classicContextMenu | 1 | AtlasOS L17717, L17727 |
| Steps Recorder | removable capability on the Windows features list | 1 | O&O ShutUp10++ L3942 |
| Sticky Keys and AutoPlay | input.accessibilityShortcutsOff, security.autoPlayOff | 1 | Sophia Script L12015, L12034 |
| Storage Sense and reserved storage | storage.storageSenseOn, storage.reservedStorageOff | 1 | AtlasOS L15570, L15604 |
| Storage Sense, hibernation, long paths | storage.storageSenseOn, power.hibernateOff, system.longPaths | 1 | Sophia Script L11441, L11472, L11491 |
| Store auto update and tips | updates.storeAutoUpdateOff, privacy.cloudContentOff | 1 | AtlasOS L16478, L16487 |
| SysMain and memory compression | memory.sysmainOff, memory.compressionOff | 1 | ReviOS L22596, L22623 |
| System Protection on | Changes page; turning it off is not offered on purpose | 1 | Winhance L6592 |
| System information | System info page | 1 | Winaero Tweaker L14550 |
| Tailored experiences off | privacy.tailoredExperiencesOff | 1 | AtlasOS L16192 |
| Task View and alignment | personalize.taskViewHidden, personalize.taskbarLeft | 1 | AtlasOS L17995, L18005 |
| Taskbar badges and flashing | focus.taskbarBadgesOff, focus.taskbarFlashingOff | 1 | Winhance L9135, L9144 |
| Taskbar search and alignment | policy and personalize.taskbarLeft | 1 | Winhance L9055, L9073, L9090 |
| Taskbar, Explorer view and Settings privacy | widgets, extensions, full path, sync ads, Settings privacy switches | 1 | ReviOS L21249, L21287, L21310 |
| Telemetry, error reporting, device encryption | privacy.telemetryOff, privacy.errorReportingOff, security.deviceEncryptionPrevented | 1 | ReviOS L22054, L22105, L22136 |
| Text message cloud backup off | privacy.messageSyncOff | 1 | O&O ShutUp10++ L3852 |
| This PC icon on the desktop | personalize.desktopThisPc | 1 | Winhance L7579 |
| USB selective suspend off | power.usbSelectiveSuspendOff | 1 | Winhance L6766 |
| Update basics | driver exclusion, Store auto update, Delivery Optimization, reserved storage | 1 | ReviOS L22458, L22465, L22482 |
| Verbose status and long paths | system.verboseStatus, system.longPaths | 1 | Flyoobe L25067, L25076 |
| Visual C++ runtime | apps.json has the x64 package | 1 | Sophia Script L12191 |
| Voice activation off | perm.voiceActivationOff | 1 | Winhance L7492, L7501 |
| Wake timers off | power.wakeTimersOff | 1 | Winhance L6685 |
| Wi-Fi power saving | battery.wifiPowerSavingDc | 1 | Winhance L6663 |
| Windowed optimizations, MPO, background apps | gpu.windowedOptimizations, gpu.mpoOff, background.backgroundAppsOff | 1 | ReviOS L22675, L22688, L22701 |
| Windowed optimizations, VRR, Auto HDR | gpu.windowedOptimizations, display.vrrOn, display.autoHdrOn | 1 | Winhance L5342, L5351, L5360 |
| Windows Ink Workspace off | personalize.inkWorkspaceOff | 1 | Winaero Tweaker L14629 |
| Windows Update defaults | undo of the update tweaks | 1 | WinUtil L1729 |
| Windows Update repair | Tools page | 1 | WinUtil L1458 |
| Windows Update: security settings | updates.featureUpdatesDeferred, updates.driversExcluded; quality update deferral missing (see Quality updates deferral) | 1 | WinUtil L1742 |

## Features without their own entry

Some features are described in prose or bullet lists rather than as tweaks; they were sorted by hand.

| Tool | Feature | Verdict | Note |
|---|---|---|---|
| WinUtil L2140 | Win11 ISO creator | Not sensible | builds install media; out of scope for an optimizer of the running PC |
| WinUtil L2238 | Environment report for support | Already | System info Copy and Settings > Copy log |
| WinUtil L2238 | Import and export of selections | Need | see "Export and import of a selection" |
| WinUtil L2238 | Light, dark and auto theme, logging | Already | Settings page |
| Winhance L9426 | Power plan list and own plan | Already | Gaming and Ultimate Performance plans |
| Winhance L9923 | WIM utility and custom ISO | Not sensible | out of scope |
| Sparkle L14300 | DNS changer | Already | Network page |
| Sparkle L14538 | Uninstaller for apps and programs | Already | Remove apps page (Preinstalled apps and Installed programs tabs) |
| Sparkle L14541 | Restore points: create, list, restore | Already | restore point per session, Open System Restore; a list inside the app would be a small Could |
| Winaero Tweaker L14546 | Import and export of all changes, reset per page | Need | export and import, see above; Undo all exists |
| Winaero Tweaker L14546 | Desktop shortcut per tweak | Not sensible | no value for one-time settings |
| Autoruns L14667 | Disable, jump to entry, hide Microsoft entries, signatures, VirusTotal, compare snapshot | Already | Startup page |
| Autoruns L14667 | Delete an entry | Could | PCO only disables; deleting needs a backup for undo |
| Autoruns L14667 | Autostarts of other user profiles | Edge | shared PCs only |
| Autoruns L14667 | Scan an offline Windows installation | Not sensible | out of scope |
| Autoruns L14667 | Export the list (CSV or text) | Could | useful for forum help, like the System info copy |
| Process Lasso L14721 | ParkControl (core parking editor) | Not sensible | parking off breaks X3D CCD parking and hybrid core selection (see Core parking off) |
| Bulk Crap Uninstaller L23177 | Bulk and quiet uninstall | Could | select several programs, run silent uninstall strings where known |
| Bulk Crap Uninstaller L23177 | Uninstall through msiexec when the uninstaller is broken | Could | documented msiexec /x with the product code |
| Bulk Crap Uninstaller L23177 | Uninstall manually (delete files and keys) | Not sensible | risky without the uninstaller |
| Bulk Crap Uninstaller L23177 | Find the program by its window | Edge | convenience |
| Bulk Crap Uninstaller L23177 | Restore point before uninstalling | Already | Installed programs card |
| Bulk Crap Uninstaller L23177 | Community ratings | Not sensible | sends data to a server; no telemetry rule |
| Bulk Crap Uninstaller L23177 | Disk use treemap | Could | Storage analyzer shows largest folders as a list |
| Bulk Crap Uninstaller L23218 | Filtered uninstall lists (.bcul) | Not sensible | admin automation, out of scope |
| Bulk Crap Uninstaller L23221 | Command line uninstall | Edge | automation |
| NVCleanstall L23333 | Custom driver package (pick components) | Not sensible | PCO installs no drivers; the Drivers card links to the vendor |
| NVCleanstall L23339 | Background check for new drivers | Already | Drivers card shows driver age and a Get update link |
| DDU L23360 | Clean graphics driver removal | Already | Drivers card links to DDU |
| DDU L23366 | Restart into Safe Mode before the clean | Could | see "Restart into Safe Mode" |
| DDU L23366 | Prevent driver downloads from Windows Update | Already | updates.driversExcluded (policy instead of SearchOrderConfig) |
| DDU L23372 | Command line | Not sensible | DDU itself |
| RemoveWindowsAI L23999 | Refuse to run with pending updates | Could | see "Pending restart check before changes" |
| RemoveWindowsAI L23999 | Windows Update repair | Already | Tools page |
| Flyoobe L24641 | Rule overview with apply and revert | Already | Overview, Tweaks and Changes pages |
| Flyoobe L24641 | Accounts, network, Windows Update and About shortcuts | Could | see "Shortcuts to classic Control Panel applets" |
| Flyoobe L24641 | Browser detection and default apps link | Could | link to ms-settings:defaultapps after a browser install |
| Flyoobe L24641 | Recipes (export and import) | Need | see "Export and import of a selection" |
| Flyoobe L24641 | Setup actions (local PowerShell packages) | Not sensible | runs arbitrary scripts elevated |
| Flyoobe L24641 | AI explainer for rules | Not sensible | needs an online model; every PCO item already has an explanation page |
| Flyoobe L24641 | Upgrade to Windows 11 on unsupported hardware | Not sensible | out of scope, PCO requires Windows 11 |

## Follow-ups outside the catalog

- This review did not use docs/not-included.md as a reason; every verdict is based on the effect of the feature. docs/not-included.md was updated to match on 2026-10-11: WPBT can return as a Preview tweak with proof, Teredo names the Xbox party chat and multiplayer break, Print Spooler and Search indexing note what is still possible, and four rows were added (processor idle states off, Fault Tolerant Heap off, registry cleaning, netsh TCP global tweaks). Checking the sources changed two verdicts of this review back to Not sensible: network in Modern Standby (Windows already keeps it quiet on battery) and handwriting data sharing (no current Microsoft policy page).
- The automatic match found the Debloat entry for PC Manager under a different package name than Flyoobe uses (see Need to implement).

## Status after implementation (2026-10-11)

- Done and proven in the VM (apply and undo checked on screen): Game Bar recording off by policy and Settings without Home (both documented, Pro and higher), File Explorer without Home and Gallery, user folders in This PC, Xbox button does not open Game Bar, wallpaper without JPEG recompression, blue screen with the stop code parameters.
- Done as Preview: silent app installs off (effect shows only over time).
- Done: export and import of the changes, older Visual C++ runtimes and the DirectX June 2010 runtime on the Apps page, the PC Manager package name and four more removable apps.
- Dropped after the VM test (`docs/not-included.md`): Paint generative erase and background removal values (no effect in Paint 11.2605) and Text and image generation through ConsentStore (Settings ignores the value on build 26300). WPBT stays out: a Hyper-V VM has no WPBT table to test it.
- Not found in any source: `LockScreenOverlaysDisabled` (`docs/TODO.md`).
- The other session fixed `privacy.locationOff` (documented DisableLocation policy), `privacy.inkingTypingOff` and `gpu.gameDvrOff`; none of the three is a Preview any more.
- Not done by owner decision: per-game power plan switching, because it needs a resident component.
