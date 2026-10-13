# Open items

What is still open after the audit of 2026-10-13 ([audit-2026-10-13.md](audit-2026-10-13.md)) and its fixes. Every high finding and every medium marked as a 1.0 blocker there is fixed in code; what follows needs a person, a VM image or hardware we do not have, or is planned work.

## Before 1.0

- **Backup update key:** the code accepts a second key (`update-key-backup.pem`). The key pair itself is the owner's step: `scripts/new-update-key.ps1 -Backup -PrivateKeyFile <USB stick>`, then commit the public half. Also copy it into the Scoop bucket's `keys/`.
- **VM runs we could not do yet:** the core plan on Windows 11 25H2 Pro and on a Home edition (needs those VM images), and once on 24H2 to see the "no longer supported" note with a working Undo.
- **New VM plan rows** (section 1 and 6 of [vm-test-plan.md](vm-test-plan.md)): small screen at 150 %, high contrast, clickable sources, a failed start after a self-update, the Startup page's task rows, Update all with machine and user scope.
- **Release:** rename `## Unreleased` in CHANGELOG.md to `## 1.0.0`, drop "(preview)" and the README status banner, decide on code signing (SignPath declined on 2026-10-13), and get the first winget and Chocolatey versions through review or keep them marked pending.
- **Accessibility pass with Narrator and the keyboard only** over Overview, Tweaks, the confirmation and Changes.

## Checks in the VM (from earlier runs)

- **`privacy.locationOff` on Home editions:** the tweak uses the documented policy "Turn off location" (`DisableLocation`), proven on Pro, and is left out on Home. Check on a Home VM whether Windows honors the policy there; if it does, drop the edition condition.
- **`focus.lockScreenTipsOff` turns off Windows spotlight:** check whether writing only `SubscribedContent-338387Enabled` avoids it.
- **Need real hardware or traffic capture:** `gpu.windowedOptimizations` (discrete GPU), `display.autoHdrOn` and `display.vrrOn` (HDR or VRR display), `network.dohAutoUpgrade`, the NVIDIA settings, MSI mode and interrupt affinity, the network adapter properties (section 5 of the VM plan).
- **Program start priorities** (Preview): the row in section 6 can be run in the VM today; with a result they leave Preview.
- **V-Cache driver:** the service name `amd3dvcacheSvc` needs a check on an X3D PC.

## Code

- **Refactors from the audit:** a Core `ChangeSession` shared by the window and the command line (also unblocks CLI verbs and an opt-in re-apply task); `UpdateService` and `UninstallService` out of `MainViewModel` with an App test project; one `ProcessLauncher` and `PowerShellScript` builder; pages behind an `IShell` interface with a page registry.
- **Engine text:** `Describe()` returns English that `labels.json` parses back with patterns; return a key and arguments instead, with a test over every action type. Halves the work for a third language (one language list instead of `de` branches).
- **Catalog model:** an "also for new users" scope (Default profile), an action for the app's own scheduled task (undo removes it), whole-key backup, and `{nic}` expansion as a method of the actions instead of a switch in the engine.
- **Start time:** measure from the launch (the bundle starts itself twice for the protected runtime folder); try ReadyToRun again against the exe size.

## Toward "the only PC optimizer you need"

In the order of the audit's promise gaps:

1. A larger app installer list (the gaming and utility shortlist of the 2026-10-11 comparison with 17 tweak tools, about 60 entries) and a winget search for any package.
2. Bulk and quiet uninstall, a leftover scan after an uninstall, orphaned Program Files folders.
3. Process Lasso style rules: prove the start priorities, add the page priority (proof needed), and say on the not-included page why live rules and affinity are left out.
4. Autoruns completeness: the missing locations (icon overlays, ShellServiceObjectDelayLoad, SharedTaskScheduler, copy hooks, Command Processor AutoRun, exefile and cmdfile open commands, SilentProcessExit, the WOW6432Node IFEO key, Winlogon Taskman, AppSetup and notification packages, credential providers, AppCertDlls, DirectShow filters), Delete with a backup, export as CSV or text.
5. About 25 documented policies left over (AI permissions, speech model updates, cross-device resume, desktop Spotlight, sync per category, Edge promotions, quality update deferral and more).
6. About 30 cosmetic tweaks (menu delays, drive letters first, shortcut suffix, Photo Viewer, Folder Options, taskbar on several displays, Num Lock, startup sound and others); many need VM proof.
7. Export and command line: removed apps, service start types and startup entries in the export; `--export`, `--profile ... --apply-recommended` and `--include-preview`.
8. An "also for new users" scope, an opt-in re-apply task after feature updates, and small quick fixes (pending restart check, Defender signature update, winget repair, icon cache reset, Restart into Safe Mode).
