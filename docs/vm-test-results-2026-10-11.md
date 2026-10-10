# VM test results, 2026-10-11

Checks handed over by the third audit ([audit-2026-10-10-3.md](audit-2026-10-10-3.md), "Status after the fixes"): the Preview tweaks that need a check of their visible effect, the visible-effect proof for `gpu.gameMode` (M15), L-D8, and the checks run in a second VM (H4, L-P4, the value-only and restart tweaks, the fixed benchmark, throttle check and storage analysis; see [Second VM](#second-vm)).

## Setup

- Hyper-V VM `PCO-Test` as in the [last run](vm-test-results-2026-10-10.md), restored from the `clean` checkpoint: Windows 11 Pro 26H2, English, build 26300.9550, not activated, 8 GB memory (no check in this run needs 16 GB).
- Version 0.5.0 at commit baa7de8: the published single exe for the app checks, and the console runner from the last run (real `TweakEngine`, real adapters, secured backup store; it refuses to run outside a Hyper-V VM) built from the same commit for apply and undo.
- Each tweak: the state where a user sees it, apply, the state again (after signing in again where the tweak needs it), undo, the state again, and the detected state before and after. Settings pages were read through UI Automation in the signed-in session (the Settings app closed and opened again for every read), classic dialogs and the taskbar from screenshots, File Explorer from a new window on a test folder. Tweaks shown on the same page ran as one group.

## Summary

| Result | Tweaks |
|---|---|
| Visible effect confirmed, no longer Preview | 28 (27 here, `security.lsaProtection` in the second VM) |
| Visible effect confirmed, extra proof for a tweak that is not a Preview | 1 (`gpu.gameMode`) |
| No effect or a side effect, stays Preview | 2 (F1, F2) |
| Effect only partly visible in the VM, stays Preview | 3 |
| Not applicable in this VM | 1 (`gpu.windowedOptimizations`, needs a discrete GPU) |

## Findings

### F1. `privacy.locationOff` has no effect

Apply writes `Deny` to `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location`, and the engine reads the tweak as applied. Settings > Privacy & security > Location still showed "Location services" On, also after signing in again and after a restart, and a desktop app still got location access (`GeoCoordinateWatcher.Permission` Granted). Turning the switch off in Settings changed none of the values in that key, the user's key or the `lfsvc` configuration, so the switch is stored elsewhere on this build. Stays Preview; the value behind the Settings switch is still to be found. The explanation pages now say that the test showed no effect.

### F2. `focus.lockScreenTipsOff` turns off Windows spotlight

Apply changed "Personalize your lock screen" from Windows spotlight to Picture; the "Get fun facts, tips, tricks, and more on your lock screen" check box only exists with Picture or Slideshow and showed Off. Undo set Windows spotlight again. The explanation pages do not say that the tweak ends Windows spotlight. The explanation pages now describe the side effect. Stays Preview until the side effect is avoided (for example by writing only `SubscribedContent-338387Enabled`, which is to be checked) or accepted.

### F3. `gpu.gameMode` shows as on while Game Mode is off

With Game Mode turned off in Settings, Windows clears only `AutoGameModeEnabled`; `AllowAutoGameMode` stays 1. The engine then detects the tweak as Partial: the row says "Partly on" with a problem status and the switch is on, while Windows shows Game Mode Off. The label is honest, so nothing was changed; turning the switch on again would write both values. The visible-effect check itself passed (see the table).

### F4. L-D8: DISM lines on the Health page lose their umlauts on German Windows

The Health page streams DISM /ScanHealth and /RestoreHealth through `StreamingProcess`, which decodes UTF-8 by default. DISM writes the OEM code page when its output is redirected: on the German host, `dism /?` returned the bytes 0x81, 0x94 and 0xE1 (ü, ö and ß in code page 850, as in "Ausführen", "erhöhte", "abzuschließen"), which are not valid UTF-8, so each umlaut shows as a replacement character. SFC is decoded correctly (it writes UTF-16, which `ScanNowAsync` passes). Checked on the host with the read-only `/?` output, since the VM has an English Windows. Fixed: the two DISM calls in `HealthTools` now read the OEM code page (`StreamingProcess.Oem`, which `SystemProcessRunner` uses too), with a test of the decoding.

## Preview checks

| Tweak | Where | Result |
|---|---|---|
| `explorer.endTask` | Settings > System > Advanced; taskbar right-click menu of Notepad | OK. "End task" On after apply, Off after undo; the menu showed "End task" only while applied. |
| `personalize.taskbarLeft` | Settings > Personalization > Taskbar; taskbar | OK. "Taskbar icon alignment" Left, Start at the left edge; Center after undo. |
| `personalize.clockSeconds` | Taskbar settings; clock | OK. Switch On and the clock showed seconds; Off after undo. |
| `personalize.taskbarNeverCombine` | Taskbar settings; taskbar after signing in again | OK. "Combine taskbar buttons and hide labels" Never; the buttons showed labels after signing in again (not right after apply); Always and combined after undo and signing in. |
| `focus.taskbarFlashingOff`, `focus.taskbarBadgesOff`, `personalize.showDesktopCornerOff` | Taskbar settings | OK. Each switch Off after apply, On after undo. |
| `personalize.snapAssistOff`, `personalize.snapLayoutsOff` | Settings > System > Multitasking | OK. The suggestion switch and both snap layout switches Off after apply, On after undo. |
| `focus.altTabTabsOff` | Multitasking settings | OK. "Show tabs from apps when snapping or pressing Alt+Tab" showed "Don't show tabs", locked by the policy; "3 most recent tabs" after undo. |
| `personalize.darkMode`, `personalize.accentTitleBars` | Settings > Personalization > Colors | OK. "Choose your mode" Dark and the title bar switch On after apply; Light and Off after undo. The page is grayed out because Windows is not activated, but shows the values. |
| `explorer.hiddenFiles` | New File Explorer window on a folder with a hidden file | OK. The hidden file was listed only while applied. |
| `explorer.compactView` | New File Explorer window | OK. Rows 22 pixels high after apply, 24 before and after undo. |
| `explorer.itemCheckboxes` | New File Explorer window, item selected | OK. The item had a check box only while applied. |
| `explorer.fullPathTitle` | New File Explorer window after signing in again | OK. Title "C:\Test\vis - File Explorer" after apply, "vis - File Explorer" before and after undo. |
| `explorer.syncProviderNotificationsOff` | File Explorer Options > View | OK. "Show sync provider notifications" unchecked after apply, checked after undo. |
| `focus.notificationSoundsOff`, `focus.finishSetupOff` | Settings > System > Notifications | OK. Both Off after apply, On after undo. |
| `personalize.dynamicLightingOff` | Settings > Personalization > Dynamic Lighting | OK. "Use Dynamic Lighting on my devices" Off after apply, On after undo (grayed out without a lighting device). |
| `personalize.communicationsDuckingOff` | Sound > Communications | OK. "Do nothing" after apply, "Reduce the volume of other sounds by 80%" (the default) after undo. |
| `personalize.desktopThisPc` | Desktop after signing in again | OK. This PC icon on the desktop while applied, gone after undo and signing in. |
| `privacy.searchHistoryOff` | Settings > Privacy & security > Search permissions | OK. "Search history on this device" Off after apply, On after undo. |
| `input.printScreenSnippingOff` | Print Screen key on the VM's virtual keyboard, after signing in again | OK. The key opened the Snipping Tool before apply and after undo, and did not while applied. The switch is no longer on Accessibility > Keyboard in this build. |
| `network.dns.opendns`, `network.dns.adguard` | Settings > Network & internet > Ethernet; `Get-DnsClientServerAddress` | OK. "DNS server assignment" Manual with the preset's servers after apply, Automatic (DHCP) after undo. |
| `network.dns.automatic` | Ethernet settings, from manual servers 8.8.8.8 and 8.8.4.4 | OK. Automatic (DHCP) after apply, Manual with 8.8.8.8 again after undo. |
| `gpu.gameMode` (M15) | Settings > Gaming > Game Mode, turned off in Settings first | OK. Off, On after apply, Off after undo. See F3. |
| `network.dohAutoUpgrade` | `netsh dns show encryption`, Ethernet settings | Partly. Windows reported auto-upgrade yes for the six servers after apply and no after undo, but Settings kept showing 8.8.8.8 as "(Unencrypted)", and HTTPS connections to 8.8.8.8 existed before apply too, so the traffic could not be told apart. |
| `display.autoHdrOn` | Settings > System > Display > Graphics | Partly. The Auto HDR switch appeared, On, only while applied (and turned "Optimizations for windowed games" on, as Auto HDR needs it); the VM has no HDR display, so the effect in games is not visible. |
| `display.vrrOn` | Graphics settings | Not visible: the VM display does not support variable refresh rate, so Settings shows no switch. |
| `privacy.locationOff` | Location settings, a desktop app's access | No effect, see F1. |
| `focus.lockScreenTipsOff` | Settings > Personalization > Lock screen | Side effect, see F2. |
| `gpu.windowedOptimizations` | | Not applicable: no discrete GPU in the VM. |

Every apply reported Applied, every undo succeeded, and every tweak was detected as not applied again after undo.

## Decision

- **No longer Preview (28):** the tweaks marked OK above, and `security.lsaProtection` from the second VM. Their catalog entries have a `proof` that points to these results, and the explanation pages of the 27 above say that a test confirmed the effect.
- **Extra proof (1):** `gpu.gameMode`.
- **Still Preview:** `network.throttlingIndex` (nothing in Windows shows it), `security.deviceEncryptionPrevented` and `expert.legacyBootMenu` (Expert, always Preview; both round trips OK in the second VM), `privacy.locationOff` (F1), `focus.lockScreenTipsOff` (F2), `network.dohAutoUpgrade`, `display.autoHdrOn` and `display.vrrOn` (need a check with traffic capture or an HDR or VRR display), `gpu.windowedOptimizations` (needs a discrete GPU).

## Second VM

A copy of the `clean` checkpoint ran as `PCO-Test2` (8 GB) at the same time, for the checks that need restarts or run outside the desktop. Full results: [vm-test-results-2026-10-11-b.md](vm-test-results-2026-10-11-b.md). In short:

- **Tweaks:** `network.throttlingIndex`, `security.deviceEncryptionPrevented` (blocked without Expert mode), `expert.legacyBootMenu` (two restarts, BCD exported first) and `security.lsaProtection` (Core isolation switch and Wininit event 12 across three restarts) all completed apply and undo with no backup left.
- **H4:** each .NET diagnostics variable in the environment of an elevated start showed the message naming the variable, and the app exited with code 1; a harmless `DOTNET_` variable did not stop it. With `DOTNET_EnableEventPipe=1` the runtime still wrote a trace file before the check ran, the limit SECURITY.md describes.
- **L-P4:** elevated processes ignore per-user `HKCU\Software\Classes` overrides of `WScript.Shell` and `Schedule.Service` (CLSID and ProgID); medium integrity processes use them. Not exploitable against the elevated app.
- **Fixed code:** the frame time benchmark captured frames (55.1 FPS; in this VM only WPF windows present frames PresentMon sees), the throttle check gave the expected verdict, and the storage analysis left duplicates in the default `XboxGames` folder alone. The refusal before the first scan finished could not be timed and is not confirmed.
- **Found and fixed:** the English UI showed decimal commas ("52,6 GB", "55,1 FPS"). The app set its culture inside the asynchronous start method, and the culture is an async local, so the change was undone at the first wait; the language is now set before that method.

## Notes for the next run

- The scripted Print Screen key press from inside the session does not reach the shell; the VM's virtual keyboard (`Msvm_Keyboard.TypeKey`) does.
- The classic Win32 dialogs (File Explorer Options, Sound) expose no UI Automation patterns to the scripted session; screenshots work.
- A copy of the VM must keep the copied key protector: a new one (`Set-VMKeyProtector -NewLocalKeyProtector`) makes the virtual TPM state unreadable and the VM does not start.
