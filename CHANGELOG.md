# Changelog

## Unreleased

Fixes from the first VM test run ([results](docs/vm-test-results-2026-10-10.md)) and from a code audit (open items in [TODO](docs/TODO.md)), plus a speed pass.

### Speed

- A scan takes about 2 seconds instead of 15: the TPM is read once (through the TPM Base Services API when the app is not elevated) instead of waiting twice for a slow WMI query, firmware, startup and extra checks run in parallel, and a rescan after a change reuses the parts no change can affect.
- The page is ready before the 3 second background CPU sample ends; the background activity check follows when the sample is done, without reading every tweak state again.
- Reading all tweak states takes under a second instead of about 8: NVIDIA driver settings share one driver session for a few seconds instead of opening one per setting, and backups are read once per pass.
- The Tweaks and Advisor lists build only the cards in view. Opening Tweaks, switching the category, the profile or "Only recommended" and turning on Expert mode take 3 to 6 times less time.
- Explanations in the details pane are built in the background, and slow work (process lists, page loads, changes) no longer runs on the UI thread.

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
- Failures show as errors instead of information. Explorer restarts ask first. Frame time captures can be stopped. Escape closes message windows. Settings are saved atomically.

### Content

- Tweaks that use an undocumented value are marked "Undocumented value" and either cite proof that it works or stay in Preview; the lint enforces it.
- Explanation pages need a Sources section, and every source must be cited in the text.

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
