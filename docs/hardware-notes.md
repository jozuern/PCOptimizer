# Notes from real hardware

Read-only checks on one gaming PC: Intel Core i7-9700K, ASUS ROG Strix Z390-F, NVIDIA RTX 2070, Windows 11 26H2 build 26300.9550. Date: 9 October 2026. Nothing was applied or undone on this PC; apply and undo are tested in a VM ([vm-test-plan.md](vm-test-plan.md)).

## Platform and firmware

- **Build / Insider:** 26300.9550 is the 26H2 release build. `WindowsSelfHost\Applicability` contains only WNS/UI values, with no `BranchName` and no `EnablePreviewBuilds`, so the PC is detected as "not Insider", which is correct. Check `winver` before running the Insider tests.
- **Microcode:** `Update Revision` and `Previous Update Revision` are 4-byte REG_BINARY values on this build (`F8 00 00 00` = 0xF8), not the 8-byte MSR copy. `Firmware Record Version` (DWORD, 248 = 0xF8) is the revision the BIOS loaded. `Preferred Record Version` (222) appears to be the revision inside Windows' microcode package. The parser handles both layouts.
- **PCIe properties:** `DEVPKEY_PciDevice_CurrentLinkSpeed/Width` = pid 9/10 and `MaxLinkSpeed/Width` = pid 11/12 are confirmed. RTX 2070 at idle: Gen 1 x16, max Gen 3 x16. The Z390 CPU root port (8086:1901) exposes no link properties, so the slot maximum is unknown on this board and a reduced width cannot be classified as "slot limited" or "trained down".
- **ReBAR:** the largest memory BAR of the RTX 2070 is 256 MB, and the catalog marks the card as unsupported. The app reports "Not supported" with no BIOS advice.
- **RAM:** `ConfiguredClockSpeed` 3200 on both CMW32GX4M2E3200C16, locators `ChannelA-DIMM2` / `ChannelB-DIMM2`, so XMP is on, dual channel.
- **MDM false positive (fixed):** unmanaged PCs carry built-in `Enrollments` entries ("Deploy/Cloud/Local Authority", types 28/30/31, state 1). Only EnrollmentType 6 or 13 counts as MDM.
- `ProductName` in the registry still reads "Windows 10 Pro" on Windows 11. The UI never shows it.

## Storage, displays and games

- **Game libraries:** `E:\SteamLibrary` and the Xbox `.GamingRoot` on E: are on a WD20EZAZ (SMR hard disk), so the "game library on a hard disk" finding fires as expected.
- **Displays:** only the AOC 27G1G4 was active during the test: 1920x1080 at 144 Hz, which is OK. The EDID preferred timing is 60 Hz and the range limit is 48 to 144 Hz.
- **Cleanup sizes:** the shader caches are 34.5 GB on this PC, which is why they are never preselected.
- **AppX:** 97 packages; of the debloat list only Camera, Teams and Xbox Game Bar are still installed.

## Tweak detection

- Many tweaks are already set on this PC (likely from an earlier tool): telemetry, activity history, Widgets, background apps, Game DVR, mouse acceleration, file extensions, classic context menu, fast startup and hibernation off, SysMain disabled. The engine shows them as "On" without an undo entry, because the app did not make those changes and does not know the originals.
- `visual.bestPerformance` showed "Partly on": some of its values match, others do not.
- The page file is set manually, so "Page file managed by Windows" is recommended.
- Without admin rights: `Get-MMAgent` fails (memory compression shows as not supported) and `bcdedit /enum` fails (boot configuration tweaks show as unknown). The app shows one banner on the Tweaks page instead of a note per row.
- System Protection state needs admin rights for the WMI class; without them the Changes page says "unknown".

## Drivers, startup and health

- **NVAPI driver settings:** driver 617.42 accepts the 12320-byte `NVDRS_SETTING_V1` that every public nvapi.h defines and rejects a 12328-byte variant with status -9. The global profile has no own values for the read settings (status -160, driver defaults).
- **VRR:** NVAPI reports "VRR not possible" for the AOC 27G1G4 although its EDID range (48 to 144 Hz, continuous frequency) looks like a FreeSync panel. The app reports this as information with the usual causes (monitor menu setting, connection), not as a problem.
- **Startup scan:** 220 entries (10 Run keys, 1 Startup folder item, 57 logon and boot tasks, 20 shell extensions, 2 Winlogon values, 84 drivers, 46 automatic services); all 46 service files resolved to existing files.
- **Signatures:** ntoskrnl.exe and tcpip.sys verify as embedded signatures, notepad.exe through the Windows catalog; a non-executable file reports "not signed".
- **Health counters:** `% Performance Limit` reads 100 at idle and `% Processor Performance` about 127 (boost), as expected. Drive reliability counters and processor sensor values need administrator rights; without them the app says so.
- **Single-file runtime:** a published test build (asInvoker manifest) restarts once and loads the WPF native libraries from `%LocalAppData%\PCOptimizer\runtime` (elevated: `%ProgramData%\PCOptimizer\runtime`) instead of `%TEMP%\.net`. The exe's own imports are KnownDLLs or API sets, and its load configuration limits delay-loaded DLLs to System32.

## Not checked on hardware

No such devices here: Wi-Fi band detection (adapter disconnected), AGESA string and AM4 fTPM rule (Intel PC), AMD chipset and Adrenalin items, Intel APO and DTT service names, laptop-only checks (power mode on battery, hybrid graphics, embedded DisplayPort panels), FACEIT and Javelin service names (`verified: false` in the catalog where no vendor page names them).
