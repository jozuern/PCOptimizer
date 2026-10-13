# Open items

What is still open after the audit of 2026-10-13 ([audit-2026-10-13.md](audit-2026-10-13.md)) and its fixes. Every high finding and every medium marked as a 1.0 blocker there is fixed in code; what follows needs a person, a VM image or hardware we do not have, or is planned work.

## Left from the 1.0 list

- **VM run on a Home edition:** the core plan on Windows 11 Home (26H2, from the official ISO). 25H2 and 24H2 are not run: Microsoft offers only the current release, and the owner decided against building older images from update files (2026-10-13).
- **New VM plan rows** (section 1 and 6 of [vm-test-plan.md](vm-test-plan.md)): a failed start after a self-update (needs a signed release newer than the installed one: the update to 1.0.1).
- **Code signing:** apply to the SignPath Foundation again once the project is better known (declined 2026-10-13 for too little public visibility); Azure Trusted Signing takes only companies, not individuals. Until then the exe stays unsigned. The first winget and Chocolatey versions are in review.

## Checks in the VM (from earlier runs)

- **`privacy.locationOff` on Home editions:** the tweak uses the documented policy "Turn off location" (`DisableLocation`), proven on Pro, and is left out on Home. Check on a Home VM whether Windows honors the policy there; if it does, drop the edition condition.
- **`focus.lockScreenTipsOff` turns off Windows spotlight:** check whether writing only `SubscribedContent-338387Enabled` avoids it.
- **Need real hardware or traffic capture:** `gpu.windowedOptimizations` (discrete GPU), `display.autoHdrOn` and `display.vrrOn` (HDR or VRR display), `network.dohAutoUpgrade`, the NVIDIA settings, MSI mode and interrupt affinity, the network adapter properties (section 5 of the VM plan).
- **V-Cache driver:** the service name `amd3dvcacheSvc` needs a check on an X3D PC.

## Code

- **Catalog model:** an "also for new users" scope (Default profile) and an action for the app's own scheduled task (undo removes it).

## Toward "the only PC optimizer you need"

In the order of the audit's promise gaps:

1. Installer: done (91 reviewed apps and a winget search for any package, passed in the VM).
2. Bulk Crap Uninstaller: done (bulk and quiet uninstall, the leftover check, unused Program Files folders, all passed in the VM).
3. Process Lasso: done as far as Windows allows without a background program (start priorities for CPU, disk and memory, all proven in the VM); live rules and affinity are on the not-included page.
4. Autoruns: done (the locations, the CSV export and Delete with a backup for registry values, Startup folder files and tasks of other programs, passed in the VM).
5. Documented policies: a cross-check of every policy value in the tool reference against the catalog (2026-10-13) left few with an effect. Added: Start without Recommended (checked on Pro), Click to Do off. Already in: quality and feature update deferral, restart scheduling, settings sync, Recall. Left out: sync per category (Settings has its own switches), AllowDeviceNameInDiagnosticData (off by default), the Settings agent and agent policies (Enterprise only), TurnOffWindowsCopilot (deprecated), TargetReleaseVersion (needs the version of the moment, not a fixed value).
6. Cosmetic tweaks: snapping (Snap Assist, layouts) and taskbar on several displays are in. Menu show delay and mouse hover time are in (a `systemParameter` action, checked in the VM). Drive letters first and the "- Shortcut" suffix are in (proven in the VM without a sign-out). Num Lock at sign-in and the startup sound use undocumented values that only show at the sign-in screen or as sound, which a VM script cannot check: they stay out until someone checks them at the console. Photo Viewer associations, context menu entries one by one and Send To stay out for now (shell extensions can already be blocked on the Startup page).
7. Export: removed apps, service start types and startup entries as opt-in sections of the changes file (`--export` and `--apply-recommended` are done). `--include-preview` stays out: a preview has not been tested on real Windows and needs its own confirmation in the app.
8. An "also for new users" scope and an opt-in re-apply task after feature updates (the quick fixes are done: restart pending check, Defender update, winget source reset, icon cache, Advanced startup for Safe Mode; their VM row is open).
