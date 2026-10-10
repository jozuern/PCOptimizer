# VM test results, 2026-10-11 (part B)

Second VM `PCO-Test2`, a copy of the `clean` checkpoint of `PCO-Test`: Windows 11 Pro build 26300.9550, English display language with a German keyboard, 8 GB, virtual TPM, no GPU (Hyper-V basic display). App 0.5.0+baa7de8 (published single exe). Tweaks were applied and undone through the console runner (`PcoVmRunner`, the real `TweakEngine` with the app's adapters); the app was driven through UI Automation in the signed-in session, elevated. Part A (Settings and Explorer checks, DNS, DoH, German DISM output) is in the other results file of this date.

## Tweaks

| Tweak | Result |
|---|---|
| `network.throttlingIndex` | OK. `NetworkThrottlingIndex` 10 (DWORD) before, 4294967295 after apply (state Pending restart), 10 after undo, no backup left. Value only: nothing in Windows shows the setting, so it stays a Preview. |
| `security.deviceEncryptionPrevented` | OK. Blocked without Expert mode (`block.expertMode`). With Expert mode: `PreventDeviceEncryption` not set before, 1 after apply, not set after undo; the `BitLocker` key stays as it was. |
| `expert.legacyBootMenu` | OK. `bootmenupolicy` Standard before. Apply exported the BCD store first (`exports\bcd-*.bcd`), set Legacy (Pending restart); after a restart Windows started normally, Legacy, state Applied. Undo set Standard; after another restart Windows started normally, Standard, state Not applied. |
| `security.lsaProtection` | OK. Clean 26300 has it on (`RunAsPPL` 2, `RunAsPPLBoot` 2; Wininit event 12 "LSASS.exe was started as a protected process with level: 4"; Windows Security > Core isolation shows "Local Security Authority protection" On). Turned off by hand (`RunAsPPL` and `RunAsPPLBoot` 0) and restarted: no event 12, the switch shows Off, the app shows Not applied. Apply wrote only `RunAsPPL` 2 (Pending restart); after a restart Windows also set `RunAsPPLBoot` 2, event 12 came at boot, the switch showed On, state Applied. Undo wrote `RunAsPPL` 0; after a restart Windows reset `RunAsPPLBoot` to 0, no event 12, the switch showed Off, state Not applied. |

## Security checks

- **Diagnostics variables at an elevated start (third audit H4): OK.** The published exe was started elevated with each variable set in its environment:
  - `DOTNET_DiagnosticPorts` (a pipe, `nosuspend`), `DOTNET_EnableEventPipe=1`, `CORECLR_ENABLE_NOTIFICATION_PROFILERS=1` and `COMPlus_DiagnosticPorts`: each showed the error box naming that variable ("A .NET diagnostics setting is set in your environment variables (DOTNET_DiagnosticPorts). ... Nothing was changed.") and the process exited with code 1.
  - `DOTNET_CLI_TELEMETRY_OPTOUT=1` (harmless): the app started normally.
  - With `DOTNET_EnableEventPipe=1` the runtime had already written `C:\Windows\System32\trace.nettrace` (457 KB) before the app's check ran (the elevated process's working folder). This is the limit SECURITY.md describes: the refusal stops the app, not what the runtime does before `Main`. The file was deleted after the test.
- **Per-user COM registrations (third audit L-P4): not honoured when elevated.** A per-user override under `HKCU\Software\Classes` was tested for `WScript.Shell` and `Schedule.Service`, once as an override of their CLSID's `InprocServer32` and once as a ProgID that points to another CLSID, both to a DLL that does not exist:
  - In a non-elevated (medium integrity) process, `New-Object -ComObject` failed with 0x8007007E for both, so the per-user entry was used.
  - In an elevated (high integrity) process, both objects were created from the machine registration.

  Windows ignores the user's classes for elevated processes, so the elevated app cannot be made to load a user's DLL this way. The overrides were removed after the test.

## Fixed code (third audit)

- **Frame time benchmark (H2): OK.** PresentMon's real `--v1_metrics` header in this VM starts `Application,ProcessID,SwapChainAddress,Runtime,SyncInterval,PresentFlags,Dropped,TimeInSeconds,msInPresentAPI,msBetweenPresents,...` (lowercase `ms`).
  - In this VM only WPF windows present frames that PresentMon sees. Edge and an animated WPF window hosted by PowerShell showed no frames even when PresentMon ran by hand, so "No frames captured" was right for them.
  - A second, renamed copy of the app (`PcoCopy.exe`) does present frames. Capturing it from the Health page gave run A1 with 55.1 FPS, and no capture file was left in `tools\captures`.
  - Before the fix, every capture showed "No frames".
- **Throttle check (M8): runs, verdict as designed.** A 120 second run on the VM with an animated page open gave "Samples: 120. Processor busy 44 %, limited 0 % of busy time. Graphics card limits were not reported ... No throttling found." 44 % busy is above the 20 % needed for a verdict, so "No throttling found" is the right text here. The "not busy enough" text did not come up in this run; its rule is covered by `BenchmarkHealthTests`.
- **Storage analysis (M9): game folders protected.**
  - Test data: two identical 300 MB files in `C:\XboxGames\TestGame\Content`, a `.GamingRoot` marker in `C:\` that the app cannot parse, and two identical 300 MB files in `C:\Test\dups`.
  - The analysis of C: (206 067 files) listed only the `C:\Test\dups` pair as duplicates ("Duplicates: 300 MB can be freed"). The pair in `C:\XboxGames` was not offered, so the fallback to the default `XboxGames` folder works.
  - The refusal before the first scan ("Wait until the PC scan has finished") could not be timed through UI Automation (the drive box does not expose its selection), so it is not confirmed here.

## Found during the run

- **Decimal commas in the English UI.** The app showed "52,6 GB" (storage analysis) and "55,1 FPS" (benchmark) with English display language. `Loc.SetLanguage` is meant to format numbers in the app language ("33.8 GB" in English). The VM's keyboard is German; the remote session's culture reads en-US, so the cause is still open. It is display only.
- **"We are adding some new features to Windows"** appeared as a Windows notification while the Tools page listed Windows features (DISM). It may be unrelated (Windows Update in the clean VM); worth watching in the next run.
