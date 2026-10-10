# Intel 13th/14th gen: BIOS microcode update needed

## Summary
::: status Problem
Your BIOS provides microcode older than {{min}}. Intel recommends {{min}} or newer for 13th/14th gen desktop CPUs against voltage-related degradation. Update the BIOS soon.
:::
::: status Ok
Your BIOS provides microcode {{min}} or newer, which contains Intel's fixes for the 13th/14th gen instability.
:::
::: status Unknown,Info,Unsupported
Checks that Intel 13th/14th gen desktop CPUs run BIOS microcode with Intel's fix for the instability issue.
:::

## Why it matters
Intel found that some 13th and 14th gen Core desktop processors requested voltages that were too high, which can age a clock circuit in the cores ("Vmin shift") and lead to system instability [1]. Intel addressed the root cause with microcode 0x12B, delivered through a BIOS update [1], and released 0x12F in May 2025 as a supplement for PCs that run for days under light load [2]. Intel recommends the latest BIOS with microcode 0x12F or newer together with the Intel Default Settings [4]. Because the aging builds up over time under high voltage [1], install the update as early as possible. This is a stability issue, not a performance tweak, so it is marked critical instead of getting an impact rating.

## How we detected it
We read the processor model and the microcode revision the BIOS loaded (`Firmware Record Version` in the processor's registry key; older builds: `Previous Update Revision`). Windows can load newer microcode itself, but the BIOS revision is what counts here, because the fix has to be active from power-on. Model list and minimum revision come from the catalog.

## How to fix
1. Note your board model ({{board}}) and open the board vendor's support page.
2. Download the newest BIOS that lists Intel microcode {{min}} or newer.
3. **BitLocker:** if BitLocker or device encryption is on, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update or a change to the TPM or the boot configuration can make Windows ask for it at the next start [5][6].
4. Update the BIOS following the vendor's instructions (ASUS EZ Flash, MSI M-Flash, Gigabyte Q-Flash, ASRock Instant Flash). Do not turn off the PC during the update.
5. After the update, choose the **Intel Default Settings** power profile in the BIOS if offered [4].
6. If the PC still crashes with the new BIOS and Intel Default Settings, contact the PC vendor or Intel support. Intel supports affected customers with an exchange and extended the warranty for affected processors to up to five years from purchase [3][4].

## How to check the fix
Run the scan again. The microcode from the BIOS should be {{min}} or newer.

## Sources
1. https://community.intel.com/t5/Processors/Intel-Core-13th-and-14th-Gen-Desktop-Instability-Root-Cause/m-p/1633442
2. https://community.intel.com/t5/Processors/Intel-Core-13th-and-14th-Gen-Vmin-Shift-Instabilty-Update-New/m-p/1686948
3. https://community.intel.com/t5/Processors/July-2024-Update-on-Instability-Reports-on-Intel-Core-13th-and/m-p/1617113
4. https://www.intel.com/content/www/us/en/support/articles/000102331/processors.html
5. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
6. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
