# Intel 13th/14th gen: BIOS microcode update needed

## Summary
::: status Problem
Your BIOS provides microcode older than {{min}}. Intel 13th/14th gen desktop CPUs need it to stop voltage-related damage. Update the BIOS soon.
:::
::: status Ok
Your BIOS provides microcode {{min}} or newer, which contains Intel's fix for the 13th/14th gen instability.
:::
::: status Unknown,Info,Unsupported
Checks that Intel 13th/14th gen desktop CPUs run BIOS microcode with Intel's fix for the instability issue.
:::

## Why it matters
Intel found that many 13th and 14th gen Core desktop processors (65 W and above) requested too high voltages, which can permanently degrade the chip ("Vmin shift"). Symptoms are crashes in games, shader compilation errors and failing decompression. Intel's microcode updates up to {{min}} limit these voltages. Damage that has already happened is not reversed, so the update should be installed as early as possible. This is a stability issue, not a performance tweak, so it is marked critical instead of getting an impact rating.

## How we detected it
We read the processor model and the microcode revision the BIOS loaded (`Firmware Record Version` in the processor's registry key; older builds: `Previous Update Revision`). Windows can load newer microcode itself, but the BIOS revision is what counts here, because the fix has to be active from power-on. Model list and minimum revision come from the catalog.

## How to fix
1. Note your board model ({{board}}) and open the board vendor's support page.
2. Download the newest BIOS that lists Intel microcode {{min}} or newer.
3. Update the BIOS following the vendor's instructions (ASUS EZ Flash, MSI M-Flash, Gigabyte Q-Flash, ASRock Instant Flash). Do not turn off the PC during the update.
4. After the update, choose the **Intel Default Settings** power profile in the BIOS if offered.

## How to check the fix
Run the scan again. The microcode from the BIOS should be {{min}} or newer.

## Sources
1. https://community.intel.com/t5/Processors/Intel-Core-13th-and-14th-Gen-Desktop-Instability-Root-Cause/m-p/1633442
2. https://www.tomshardware.com/pc-components/cpus/raptor-lake-instability-saga-continues-as-intel-releases-0x12f-update-to-fix-vmin-instability
