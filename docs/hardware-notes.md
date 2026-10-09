# Notes from real hardware (M1)

Values confirmed on the Gaming PC: i7-9700K, ASUS ROG Strix Z390-F, RTX 2070, Windows 11 26H2 build 26300.9550. Date: 9 Oct 2026.

- **Build / Insider:** 26300.9550 is the 26H2 GA build. `WindowsSelfHost\Applicability` contains only WNS/UI values,
  with no `BranchName` and no `EnablePreviewBuilds`, so the PC is detected as "not Insider". This is correct: the PC is not on the
  Experimental channel. (Plan v4 §1 assumed it was. Check `winver` before running the Insider tests.)
- **Microcode:** `Update Revision` and `Previous Update Revision` are **4-byte** REG_BINARY values on this build
  (`F8 00 00 00` = 0xF8), not the 8-byte MSR copy. `Firmware Record Version` (DWORD, 248 = 0xF8) is the revision the BIOS loaded.
  `Preferred Record Version` (222) appears to be the revision inside Windows' microcode package. The parser handles both layouts.
- **PCIe properties:** `DEVPKEY_PciDevice_CurrentLinkSpeed/Width` = pid 9/10 and `MaxLinkSpeed/Width` = pid 11/12 are confirmed.
  RTX 2070 at idle: Gen 1 x16, max Gen 3 x16. The **Z390 CPU root port (8086:1901) exposes no link properties**, so the slot
  maximum is unknown on this board and a reduced width cannot be classified as "slot limited" or "trained down".
- **ReBAR:** the largest memory BAR of the RTX 2070 is 256 MB, and the catalog marks the card as unsupported. The app reports "Not supported" with no BIOS advice.
- **RAM:** `ConfiguredClockSpeed` 3200 on both CMW32GX4M2E3200C16, locators `ChannelA-DIMM2` / `ChannelB-DIMM2`, so XMP is on, dual channel.
- **MDM false positive (fixed):** unmanaged PCs carry built-in `Enrollments` entries ("Deploy/Cloud/Local Authority",
  types 28/30/31, state 1). Only EnrollmentType 6 or 13 counts as MDM.
- **Game libraries:** `E:\SteamLibrary` and the Xbox `.GamingRoot` on E: are on a WD20EZAZ (SMR HDD), so F12 fires as plan v4 §10.3 expects.
- **Displays:** only the AOC 27G1G4 was active during the test: 1920×1080 at 144 Hz, which is OK. The EDID preferred timing is 60 Hz and the range limit is 48 to 144 Hz.
- `ProductName` in the registry still reads "Windows 10 Pro" on Windows 11. The UI never shows it.

## Milestone 2 (same PC, read-only detection)

- Many tweaks are already set on this PC (likely from an earlier tool): telemetry, activity history, Widgets, background apps, Game DVR, mouse acceleration, file extensions, classic context menu, fast startup and hibernation off, SysMain disabled. The engine shows them as "On" without an undo entry, because the app did not make those changes and does not know the originals.
- `latency.mmcss` and `visual.bestPerformance` are "Partly on": some of their values match, others do not.
- The page file is set manually, so "Page file managed by Windows" is recommended.
- Without admin rights: `Get-MMAgent` fails (memory compression shows as not supported) and `bcdedit /enum` fails (BCD tweaks show as unknown). The app shows one banner on the Tweaks page instead of a note per row.
- System Protection state needs admin rights for the WMI class; without them the Changes page says "unknown".

## Milestones 3 to 6 (same PC, read-only)

- **NVAPI DRS:** driver 617.42 rejects the current `NVDRS_SETTING_V1` layout (12328 bytes) with -9 and accepts the older 12320-byte layout; the session tries both. The global profile has no own values for the read settings (status -160, driver defaults).
- **VRR:** NVAPI reports "VRR not possible" for the AOC 27G1G4 although its EDID range (48 to 144 Hz, continuous frequency) looks like a FreeSync panel. The app reports this as information with the usual causes (monitor menu setting, connection), not as a problem.
- **Startup scan:** 220 entries (10 Run keys, 1 Startup folder item, 57 logon and boot tasks, 20 shell extensions, 2 Winlogon values, 84 drivers, 46 automatic services); all 46 service files resolved to existing files.
- **Signatures:** ntoskrnl.exe and tcpip.sys verify as embedded signatures, notepad.exe through the Windows catalog; a non-executable file reports "not signed".
- **Health counters:** `% Performance Limit` reads 100 at idle and `% Processor Performance` about 127 (boost), as expected. Drive reliability counters and CPU sensor values need administrator rights; without them the app says so.
- **Cleanup sizes:** the shader caches are 34.5 GB on this PC, which is why they are never preselected.
- **AppX:** 97 packages; of the debloat allowlist only Camera, Teams and Xbox Game Bar are still installed.
- Not verified on hardware (no such devices here): Wi-Fi band detection (adapter disconnected), AGESA string and AM4 fTPM rule (Intel PC), AMD chipset and Adrenalin items, Intel APO and DTT service names, laptop-only checks (power mode overlay on battery, hybrid graphics panel), FACEIT and Javelin service names (`verified:false` in the catalog).
