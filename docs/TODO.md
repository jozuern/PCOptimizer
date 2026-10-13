# Open items

What is still open after the audit of 2026-10-13 ([audit-2026-10-13.md](audit-2026-10-13.md)) and its fixes. Every high finding and every medium marked as a 1.0 blocker there is fixed in code; what follows needs a person, a VM image or hardware we do not have, or is planned work.

## Left from the 1.0 list

- **Backup update key in the Scoop bucket:** the key pair exists (public half committed as `update-key-backup.pem`, private half offline with the owner) and every workflow here checks against both keys; the bucket's pinned copy in `keys/` still needs it.
- **VM runs we could not do yet:** the core plan on Windows 11 25H2 Pro and on a Home edition (needs those VM images), and once on 24H2 to see the "no longer supported" note with a working Undo.
- **New VM plan rows** (section 1 and 6 of [vm-test-plan.md](vm-test-plan.md)): a failed start after a self-update (needs a signed release newer than the installed one: the update to 1.0.1).
- **Release:** decide on code signing (SignPath declined on 2026-10-13), and get the first winget and Chocolatey versions through review or keep them marked pending.
- **Listen once with Narrator** over Overview, Tweaks, the confirmation and Changes. The Tab order and the names it reads were checked on every page and in the confirmation with `--tab-order` and UI Automation (2026-10-13); what is left is hearing it on a desktop, for example whether a result is announced.

## Checks in the VM (from earlier runs)

- **`privacy.locationOff` on Home editions:** the tweak uses the documented policy "Turn off location" (`DisableLocation`), proven on Pro, and is left out on Home. Check on a Home VM whether Windows honors the policy there; if it does, drop the edition condition.
- **`focus.lockScreenTipsOff` turns off Windows spotlight:** check whether writing only `SubscribedContent-338387Enabled` avoids it.
- **Need real hardware or traffic capture:** `gpu.windowedOptimizations` (discrete GPU), `display.autoHdrOn` and `display.vrrOn` (HDR or VRR display), `network.dohAutoUpgrade`, the NVIDIA settings, MSI mode and interrupt affinity, the network adapter properties (section 5 of the VM plan).
- **V-Cache driver:** the service name `amd3dvcacheSvc` needs a check on an X3D PC.

## Code

- **Refactors from the audit:** `UpdateService` and `UninstallService` out of `MainViewModel` with an App test project; pages behind an `IShell` interface with a page registry.
- **Catalog model:** an "also for new users" scope (Default profile), an action for the app's own scheduled task (undo removes it), whole-key backup, and `{nic}` expansion as a method of the actions instead of a switch in the engine.
- **Start time:** measure from the launch (the bundle starts itself twice for the protected runtime folder); try ReadyToRun again against the exe size.

## Toward "the only PC optimizer you need"

In the order of the audit's promise gaps:

1. Installer: done (91 reviewed apps and a winget search for any package, passed in the VM).
2. Bulk Crap Uninstaller: done (bulk and quiet uninstall, the leftover check, unused Program Files folders, all passed in the VM).
3. Process Lasso: done as far as Windows allows without a background program (start priorities for CPU, disk and memory, all proven in the VM); live rules and affinity are on the not-included page.
4. Autoruns: done (the locations, the CSV export and Delete with a backup for registry values, Startup folder files and tasks of other programs, passed in the VM).
5. The documented policies still left over (sync per category, quality update deferral and more); AI permissions without documented values, cross-device resume, desktop Spotlight and Edge promotions were checked and stay out, with the reasons in the commit history and on the not-included page.
6. About 30 cosmetic tweaks (menu delays, drive letters first, shortcut suffix, Photo Viewer, Folder Options, taskbar on several displays, Num Lock, startup sound and others); many need VM proof.
7. Export: removed apps, service start types and startup entries as opt-in sections of the changes file (`--export` and `--apply-recommended` are done). `--include-preview` stays out: a preview has not been tested on real Windows and needs its own confirmation in the app.
8. An "also for new users" scope and an opt-in re-apply task after feature updates (the quick fixes are done: restart pending check, Defender update, winget source reset, icon cache, Advanced startup for Safe Mode; their VM row is open).
