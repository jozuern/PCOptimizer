# Changelog

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
