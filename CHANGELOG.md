# Changelog

## 0.6.0 (preview)

A pass over the interface for public use.

### Interface

- The navigation pane groups its pages under Optimize, Clean up and Maintain; System info sits next to Settings. Graphics & network and Services & tasks have icons that no longer look like the Settings gear or a phone.
- The Tweaks list has headings: recommended for this PC, changes that work against the profile, then one per impact level.
- The tags on a tweak (Preview, Undocumented value, Moderate risk, Anti-cheat and the others) explain themselves in a tooltip.
- DNS is one choice (Automatic, Cloudflare, Google, Quad9, OpenDNS, AdGuard DNS) instead of six switches of which only one could be on. Choosing another set of servers undoes the one this app set first, and "Use the fastest" after the DNS test works the same way.
- The NVIDIA profiles per game and the device interrupt settings take one line per entry, and the per-game list comes after DNS.
- The details pane and the confirmation dialog show registry paths of the signed-in user as `HKCU\...` when the app runs as that user, and a value that is not set as "not set (Windows default)".
- Tools groups its quick fixes into network fixes, restarting a part of Windows and repairing Windows; the system file check (SFC, DISM) moved there from Health, next to the Windows Update repair and the drive check.
- Startup opens on the programs that start at sign-in; every other location is still one choice away in the list.
- Apps & drivers shows a heading per category and a label while it loads, and the list of installed programs passes the mouse wheel on to the page at its top and bottom.
- Overview no longer repeats the hardware summary (System info has it), and an empty recommendation box takes one line.
- Smaller things: Cleanup dims empty categories and writes file counts with thousands separators, Debloat shows no greyed-out button when OneDrive is not installed, the app's notes on the Services page are no longer drawn like links, read-only start types show a lock, Settings has an intro and keeps the update switch and "Check now" in one card, and the Changes page names its list "Changes you can undo".

## 0.5.0 (preview)

Fixes from the first VM test run ([results](docs/vm-test-results-2026-10-10.md)) and from a code audit (open items in [TODO](docs/TODO.md)), plus a speed pass.

### Speed

- A scan takes about 2 seconds instead of 15: the TPM is read once (through the TPM Base Services API when the app is not elevated) instead of waiting twice for a slow WMI query, firmware, startup and extra checks run in parallel, and a rescan after a change reuses the parts no change can affect.
- The page is ready before the 3 second background CPU sample ends; the background activity check follows when the sample is done, without reading every tweak state again.
- Reading all tweak states takes under a second instead of about 8: NVIDIA driver settings share one driver session for a few seconds instead of opening one per setting, and backups are read once per pass.
- The Tweaks and Advisor lists build only the cards in view. Opening Tweaks, switching the category, the profile or "Only recommended" and turning on Expert mode take 3 to 6 times less time.
- Explanations in the details pane are built in the background, and slow work (process lists, page loads, changes) no longer runs on the UI thread.
- The first results show about 3 seconds after the start instead of 5.5 (published exe on the test PC, without administrator rights). The scan starts while the window is still being built, the extra checks run beside the hardware probes, the tweak reads that start PowerShell, DISM or bcdedit begin with the scan instead of one after another after it, the scheduled task list is read from each task's XML on four threads, and the steps after the scan return to the window only once.
- Switching the language builds the rows of the opened pages from what they already read, instead of reading startup entries, services, programs, drivers, AppX packages, folder sizes and Windows features again: about 0.3 seconds instead of 2.
- Switching the theme no longer builds the rows again (status colors follow the theme by themselves), and profile and Expert mode switches leave the hardware rows, and the Network rows until that page is opened, as they are: each takes about half the time.
- After the first scan, the Startup, Services, Apps, Debloat and Cleanup pages load their data in the background, one after another while the window is idle, so they open with their rows instead of a loading state. Tools still reads its Windows features when it is opened (DISM takes about 12 seconds).
- The background CPU sample starts after the scan and the tools the app started, so neither counts as background activity. The log file stays open for the session instead of being opened for every entry, and one Storage query serves the three checks that read the drives.

### VM test run of 2026-10-11

- 28 Preview tweaks are no longer Previews: a test on Windows 11 26H2 showed their effect where you see it (Settings, the taskbar, File Explorer, the desktop, the Print Screen key, the DNS settings, Windows Security) and its removal after undo ([results](docs/vm-test-results-2026-10-11.md)). Game Mode has a visible-effect proof too.
- "Location access off" showed no effect in the test and "Lock screen: no fun facts and tips" also turned off Windows Spotlight: both stay Previews, and their explanations now say so.
- The DISM checks on the Health page show umlauts again on German Windows: DISM writes the console code page, which was read as UTF-8.

### Security

- An elevated start refuses to run when the runtime folder is not protected, and files the app creates are owned by Administrators.
- Updates are checked against an ECDSA signature of the release (`PCOptimizer.exe.sig`) besides the hash; an update without a valid signature is not installed.
- Child processes (PowerShell, system tools) start in System32 with a cleaned environment, so variables like `COR_PROFILER` or `PSModulePath` cannot load foreign code into them.
- Plain DLL names load only from System32. The signature check cache is keyed to the file's identity, so a replaced file is checked again. Builds restore locked package versions, CI actions are pinned to commits, and releases are built only from `main`.

### Undo and engine

- When one step of a tweak fails, the steps already done are rolled back, including the failing step when it changed something before failing; the result says when something could not be rolled back.
- A damaged backup file is set aside (`.damaged`) and blocks a new change to that tweak instead of being overwritten. Backups are written to disk before the change starts.
- A tweak whose conditions cannot be checked on this PC is blocked instead of applied.
- Undo restores only what the tweak changed: per power source for power settings and power modes, per keyword for network adapter properties, and only the registry keys the tweak created. Power plans are restored by GUID, and a scheduled task, NVIDIA profile, DNS setting or display mode that no longer exists is skipped instead of failing.
- The restore point frequency is set back when no restore point was created.

### Checks

- RAM speed, channel layout, the AMD AGESA version, Resizable BAR platform support, the microcode age with an unknown BIOS date, X3D processors and the battery report on desktops no longer give wrong results.
- The readiness score counts only findings that matter on their own, not prerequisites of other findings.

### App

- Switches and change buttons are disabled while a change, scan or update runs, and only one change runs at a time.
- Numbers follow the app language everywhere: results that appear after a click (storage analysis, benchmark, cleanup sizes) no longer use the Windows regional format, such as "52,6 GB" on an English page.
- Failures show as errors instead of information. Explorer restarts ask first. Frame time captures can be stopped. Escape closes message windows. Settings are saved atomically.
- System info starts with tiles for processor, graphics, memory, displays, mainboard and Windows. Each area is a card with an icon and divided rows. Disks, volumes (with a used space bar), memory modules and network adapters get one row each with a second line instead of comma lists. Values are translated (graphics kind, disk health, boost mode, power plan type, HDR, G-SYNC), and IDs and raw values (PnP ID, GUID, microcode, EDID) sit behind "Technical details". "Copy" puts the whole page on the clipboard.
- Tweak rows show Preview, Undocumented value, Disputed, Restart and Sign out as tags, and risks (Moderate risk, Expert only, Boot configuration, Anti-cheat, not reversible) as tags in the caution color, instead of at the end of the detail line. Service descriptions from Windows are cut to two lines; the whole text is in the tooltip.
- The Tweaks and Startup pages have a search box. Startup shows its filters and its actions on separate rows, and each entry is one line shorter.
- With the details pane open, the switches on the right of long lists were cut off; cards now take the visible width.
- After a theme switch, switches that were on turned gray on PCs with a gray Windows accent: the accent is now set before the theme loads. "System" follows Windows through one listener for the app mode and the accent.
- Debloat says once at the top that removing apps needs administrator rights, instead of on every app. Cleanup shows the selected size and "Clean selected" above the list. "Apply recommended" is hidden while there is nothing to apply. BIOS & hardware says how many passed checks are hidden. "Show passed checks" is a switch like the other filters, and the Services and Scheduled tasks tabs mark the selected tab.

### Content

- Tweaks that use an undocumented value are marked "Undocumented value" and either cite proof that it works or stay in Preview; the lint enforces it.
- Explanation pages need a Sources section, and every source must be cited in the text.

### Second audit ([report](docs/audit-2026-10-10-2.md))

- **Security:** the data folder is created locked in one step and never takes over files another account put there. The app refuses to run elevated with a .NET profiler set in the environment, and startup hooks are off. Tools the app starts get an admin-only TEMP and Windows' own paths instead of values from the user's environment. A per-user uninstall entry for a Windows Installer package runs msiexec as the user. Uninstallers in folders with generic write rights for users no longer run elevated.
- **Shared PCs:** backups of an account's own settings are kept per account, so a second Windows account cannot take over or undo the first account's changes.
- **One change at a time:** removing apps, cleanup, uninstalls, winget, quick fixes, repairs and the sensor driver install now wait for each other, for tweaks, for scans and for the self-update.
- **Fixes:** "Move to the Recycle Bin" asks before a file too large for the Recycle Bin is deleted for good; system files such as pagefile.sys are no longer offered. The OneDrive guard checks every account. "Renew IP address" always renews after a release. The Windows Update repair reports a service that did not start. DNS over HTTPS reports a server that could not be set. Startup entries that run PowerShell through cmd or a file through rundll32 are marked, logon tasks with quoted paths and shortcuts are checked by their file, and one bad StartupApproved value no longer hides the Run entries. "RX Vega 10/11 Graphics" counts as integrated graphics. XMP and EXPO speeds reported in MHz are read correctly. Rules for program start priorities set by other programs are listed and can be removed.
- **Speed:** DISM and PowerShell reads are shared within a scan and an apply, and the hardware scan does not wait for threads on PCs with few cores.

### Third audit ([report](docs/audit-2026-10-10-3.md))

- **Security:** the elevated start also refuses .NET diagnostic ports, notification profilers, EventPipe traces and crash dumps set in the user's environment. Elevated tools find Windows PowerShell on their PATH again. Files signed for other vendors through Microsoft's driver program no longer count as Microsoft's. Startup entries that start a file through explorer, msiexec, pcalua and similar Windows programs are marked, as are PowerShell without ".exe" and scripts in alternate data streams; unquoted paths are resolved the way Windows runs them. An uninstaller run through cmd or a script host runs elevated only when the script is in an admin-only folder. Paths on network shares are never trusted.
- **Undo:** applying a tweak again keeps the undo step of the first apply, so a later catalog version cannot leave part of the change behind. A backup that lost its per-account entries leaves no stale copy. A failed apply no longer leaves entries that later show as "reset by Windows", a backup found damaged during an apply blocks it, an apply whose values could not be read changes nothing, a rollback removes only a power plan it created, and the DirectX settings value keeps everything else stored in it.
- **Checks and tools:** the frame time benchmark reads the capture of the included PresentMon (it reported "no frames" for every run). The throttle check says when the processor was not busy enough to judge it, and reports a graphics card slowed by its power supply. The storage analysis protects the real Xbox game folders and Epic, GOG and Ubisoft games, and waits for the PC scan; hard links no longer count as duplicates, and Stop ends the hashing of a large file. The background CPU check no longer overstates processes.
- **App:** turning on sensors no longer freezes the window. A failed service start type change shows the real start type again. Startup and driver rows no longer show empty parts. Switches have names for screen readers.
- **Content:** "Optimizations for windowed games", "End task" in the taskbar, Location access off and Network throttling off use values Microsoft does not document: marked as such and in Preview until tested. "Prevent device encryption" is an Expert option. Show file extensions and Transparency effects off cite the Microsoft pages that name their values.
- **Build:** the publish restore runs in locked mode again, merges raise the version, the hooks keep LF line endings, and the release signing key is kept in a GitHub environment that allows only version tags and waits for approval.

### New tweaks (documented settings)

100 tweaks added from the review of other tweak tools ([feature reference](docs/windows-tweak-tools-feature-reference.md)), each a setting Microsoft, Google or Brave documents, with an explanation page in English and German:

- **Browsers:** Microsoft Edge (diagnostic data, personalization, shopping, sidebar, setup and default browser prompts, new tab page, tips and offers, tracking prevention, address bar suggestions, error page services, network prediction, feedback, payment queries, password saving, sign-in, AI features, cloud text prediction, tab services, visual search), Google Chrome (AI features off or without model training, URL-keyed data collection, promotions, background mode) and Brave (Rewards, Wallet and VPN, Leo, analytics and pings, News, Talk and Playlist). Chrome and Brave tweaks appear only when the browser is installed. Edge pages say when Edge ignores a policy in profiles signed in with a personal Microsoft account.
- **AI features:** Recall removed, Paint and Notepad AI features off.
- **App permissions:** camera, microphone, notifications, voice activation, motion, phone calls, radios and wireless devices for Windows apps.
- **Privacy:** location service off, Windows Search without web and location, no infection reports from the Malicious Software Removal Tool, Spotlight and cloud content (Enterprise and Education), OneSettings downloads, the privacy page at sign-in, recent files, the lock screen camera, Windows Media DRM, OneDrive folder backup.
- **Windows Update and Store:** feature updates one year later, no automatic Store app updates, fewer update notifications, no automatic archiving of unused apps.
- **Personalize and Explorer:** taskbar search and Task View button, Start lists, clear sign-in background, first sign-in animation, detailed status messages, Hibernate in the power menu, Open with prompts, shortcut search, Ink Workspace, default printer.
- **Security:** Defender potentially unwanted app blocking, network protection and sandbox, LSA protection, password reveal button, LLMNR, automatic sign-in after restarts, Remote Assistance, wireless projection, AutoPlay, automatic device encryption.
- **Network:** DNS presets for OpenDNS and AdGuard, a preset that sets DNS back to automatic, automatic DNS over HTTPS for known servers, and "Use the fastest" after the DNS benchmark.
- **Other:** no wake timers, notifications and the notification center, account notifications in Start, Edge tabs in Alt+Tab (Microsoft marks that policy as a preview), reserved storage off, Sticky Keys, Filter Keys and Toggle Keys shortcuts (through the documented SystemParametersInfo flags, changed only when the app runs under your own account), long paths, Program Compatibility Assistant, OneDrive sync, SMB bandwidth throttling, indexing on battery, the classic F8 boot menu (Expert).
- Every registry and accessibility tweak in the catalog is now applied and undone in the registry sandbox by a test.

### New tweaks (values behind Settings switches, Preview)

25 tweaks for values behind a Settings or Folder Options switch that Microsoft does not document. They carry the "Undocumented value" badge and stay Preview until the VM test confirms the effect: dark mode, accent color on title bars, taskbar on the left, seconds in the clock, never combine taskbar buttons, no taskbar flashing and badges, no show desktop corner, Snap suggestions and layouts, desktop icon for This PC, Dynamic Lighting, communications ducking, notification sounds, Auto HDR, variable refresh rate for DirectX 11 full screen games, hidden files, compact view, item check boxes, full path in the File Explorer title, no sync provider notifications, no search history on this device, Print Screen copies the screen instead of opening Snipping Tool, no fun facts on the lock screen and no "Let's finish setting up your device" screen.

### Tools and pages

- **Quick fixes** on the Tools page: renew the IP address, reset TCP/IP, restart Windows Audio, Bluetooth, Windows Search or the graphics driver, sync the clock, rebuild performance counters, check the system drive online (chkdsk /scan) and turn the recovery environment back on.
- **Program start priorities** on the Tools page: a CPU priority and optionally a low disk priority that Windows applies every time a program starts, without a background program (Preview, undocumented values).
- **Windows features:** Client for NFS and optional capabilities such as PowerShell ISE, WordPad, WMIC, the OpenSSH client, Math Recognizer and Print and Scan.
- **Apps and drivers:** uninstall desktop programs (with a restore point first), "Update all" through winget, .NET Desktop Runtime 8 and 10, and a card that explains when Display Driver Uninstaller is the right tool.
- **Debloat:** 126 removable apps instead of 34, with groups for discontinued apps, third-party promotions and PC maker apps, and Edge Game Assist. The page says when a removed app came back after a Windows update.
- **Startup:** more locations (RunOnce, Active Setup, the Load value, boot execute, Known DLLs, Winsock providers, print monitors, LSA packages, network providers, codecs, WMI consumers), a snapshot to compare against later with "Only new", and buttons to show the file and copy the location.
- **Changes:** "Open System Restore" opens the Windows dialog for restore points.

### Fixes

- Explanations showed "1][2]" instead of "[1][2]" where two sources are cited side by side.

### Undo and engine (VM test run)

- Undo of "Memory compression off" did nothing: the change takes effect only after a restart, so the engine recorded the old value and later took the new one for a reset by Windows. It now shows "pending restart" after apply, and undo turns memory compression back on, also before the restart and for changes made with 0.4.0. After the undo the tweak shows "Off after a restart" until Windows restarts, and turning it on again before then works.
- A Windows default no longer blocks a conflicting tweak. With the default Balanced plan, the Gaming and Ultimate Performance plans were blocked, and with the default boost mode "Processor boost off" could never be applied. Only changes made by this app still block.
- "Hibernation off" is unsupported where the firmware cannot hibernate (virtual machines, some firmware). Before, undo failed there and the change stayed on the Changes page; such a change can now be undone.
- When Windows denies writing a protected value even with administrator rights (the Widgets policy on newer builds), the error names the value instead of "Attempted to perform an unauthorized operation".
- DNS presets and delayed TCP acknowledgements work inside Hyper-V virtual machines. Hardware properties of network adapters still change only on PCI and USB adapters.

### Pages (second VM test run)

- The three DNS presets are named after their resolver (Cloudflare, Google, Quad9). Before, all three rows read "Public DNS servers".
- Startup: a Startup folder shortcut with arguments showed "File not found" and no publisher, because the whole command line was checked as a file. Windows tasks that call rundll32 with a switch first (Autochk) showed the same.
- The storage analyzer no longer offers the page file, swap file, hibernation file and boot dump log in the root of a drive for deletion.
- 13 tweaks are no longer marked Preview: the VM test showed that apply, the visible effect and undo work ([Preview review](docs/vm-test-results-2026-10-10.md#preview-review)). Expert and boot-critical tweaks, the NVIDIA and network adapter settings, HAGS, MPO, Game DVR and inking and typing stay Preview.
- Startup entries, services, tasks and the restore point frequency show On or Off on the Changes page like the other changes.
- Counts read "Changes: 1" and "Files: 1" instead of "1 changes" and "1 files".
- The undo confirmation no longer says that the original values are saved; it says that values which cannot be restored stay on the Changes page.
- Scheduled tasks that run on a schedule are no longer titled "at sign-in or boot".
- The SFC and DISM output follows the dark theme and shows each progress line once instead of one line per percent.
- The throttle check says when the graphics card reports no limits instead of showing "? %".
- The DNS confirmation names the network adapter, and the list of removed apps shows app names instead of package names.
- Screen readers announce list and combo box items by name, the language buttons work with a screen reader's select action, and a row switch no longer waits for its confirmation to close.
- Windows Update repair stops Cryptographic Services again and retries when renaming catroot2 is denied.
- The PawnIO install text says that uninstalling it in Settings can leave the driver, and how to remove it.
- Tweaks that are off show a grey minus instead of an empty circle, which looked like a button to click; tweaks that do not apply to this PC show a prohibited sign.

## 0.4.0 (preview)

A full review of every tweak, check, data file and text against Microsoft and vendor documentation, plus security hardening. Apply and undo are still only tested against the registry sandbox; run the [VM test plan](docs/vm-test-plan.md) before relying on it.

### Security

- The data folder `%ProgramData%\PCOptimizer` is secured before the first log or settings access. A link there is removed, a folder created by another account is deleted, and files that Administrators or SYSTEM do not own are removed instead of being adopted (a planted backup could otherwise make Undo write chosen values as administrator).
- Native libraries load only from System32 or the protected runtime folder; a DLL of the same name next to the exe is refused.
- The single-file exe no longer loads its native WPF libraries from `%TEMP%`: it restarts once with the extraction folder inside the protected data folder.
- Self-update starts the new exe only if it still has the checked hash, and keeps it locked against changes until it has started. Release download links must point into this repository in canonical form.
- Only web and Microsoft Store links are opened.

### Tweaks

- 15 tweaks removed because the evidence does not support them, they use undocumented settings, or they cost more than they bring: MMCSS profile, Win32PrioritySeparation, dynamic tick off, global timer resolution, Spectre and Meltdown mitigations off, SmartScreen off, OverlayTestMode removal, fullscreen optimizations off, Teredo off, 8.3 names off, handwriting data sharing off, WPBT off, Print Spooler on Manual, core parking off, minimum processor state 100 %. Reasons and sources: [docs/not-included.md](docs/not-included.md). Changes made with them can still be undone.
- 3 tweaks added, all documented by Microsoft: drivers not included with Windows Update, no automatic download of device apps, registry backup to RegBack.
- New **Preview** marker for risky tweaks that still need testing on real Windows. Preview tweaks show a badge and a warning and are never part of "Apply recommended".
- Corrected actions: multiplane overlay off now uses NVIDIA's documented OverlayTestMode = 5; delayed TCP acknowledgements no longer write the undocumented TCPNoDelay; NIC power saving changes only the standard *EEE and *SelectiveSuspend keywords; background apps use the documented LetAppsRunInBackground policy; Recall off no longer writes the deprecated Copilot policy; best-performance visual effects change only their own bits of UserPreferencesMask; the consumer features policy is offered only on Enterprise and Education.
- Impact, evidence basis, restart flags, sources and EN/DE explanation pages corrected for every tweak. No tweak claims "measured" without a cited measurement.

### Checks

- Fixes for laptops with embedded DisplayPort panels, the primary monitor on integrated graphics, unknown AC power state, virtual network adapters, Wi-Fi details withheld without location permission, unreadable virtualization-based security state, the Raptor Lake model list, Resizable BAR support per GPU family and on laptops, Intel Arc classification, TRIM default, low disk space on data drives, four DDR5 modules on Ryzen, and checks that fail partway.
- Anti-cheat requirements now follow the vendors' own pages (Vanguard, FACEIT with VBS and IOMMU, Easy Anti-Cheat, BattlEye, EA Javelin as "sometimes").
- BIOS menu paths checked against the board makers' manuals; GPU, CPU, storage (SMR list), app and inbox app data verified.

### Undo and engine

- Changes made with a removed tweak remain undoable.
- Power plan settings keep one backup per plan, so switching plans no longer loses an original or reports a false reset; a plan the app created is removed on undo even after a plan switch.
- Undo removes only registry keys that are empty afterwards, keeps the registry type of network adapter keywords, and succeeds for services that were uninstalled.
- Interrupt affinity is offered for graphics cards only; DNS presets and per-adapter values apply to physical adapters only.

### Other

- Debloat no longer promises a Store reinstall for apps the Store dropped, and links use the Store product ID.
- winget installs the machine-wide package for apps that offer both installers.
- Counts in the README and the VM test plan tables are now checked against the catalog by tests.
