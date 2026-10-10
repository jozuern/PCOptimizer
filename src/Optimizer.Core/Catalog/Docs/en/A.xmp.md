# Memory below its rated speed (XMP / EXPO)

## Summary
::: status Problem
Your memory is rated for {{rated}} MT/s but runs at {{configured}} MT/s. Turning on {{profileName}} in the BIOS usually lets it run at its rated speed.
:::
::: status Ok
Your memory runs at its rated speed of {{rated}} MT/s. {{profileName}} (or an equivalent setting) is active.
:::
::: status Info
::: variant fourDimms
Four DDR5 modules run at {{configured}} MT/s, below the kit's {{rated}} MT/s. AMD specifies DDR5-3600 for four modules on Ryzen, so this is expected.
:::
::: variant laptop
Your memory is rated for {{rated}} MT/s but runs at {{configured}} MT/s. Most laptops fix the memory speed, so there may be no setting for it.
:::
::: variant default
Checks whether the memory runs at the speed printed in its part number, which usually needs XMP or EXPO in the BIOS.
:::
:::
::: status Unknown,Unsupported
Checks whether the memory runs at the speed printed in its part number, which usually needs XMP or EXPO in the BIOS.
:::

## Why it matters
Memory modules first start at a safe standard speed (JEDEC) [1]. The faster speed on the box is stored as a profile (Intel XMP, AMD EXPO) that must be switched on in the BIOS [1][2]. ASUS boards show **DOCP** when the kit carries the other vendor's profile, for example an XMP kit on an AMD board [3]. Intel and AMD describe these profiles as memory overclocking, and Intel notes that changing clock or voltage can affect warranties and reduce stability [1][2]. Without the profile, the memory runs slower than its rating, and games that depend on memory bandwidth and latency lose frame rate, especially in the 1 % lows.
::: variant laptop
Most laptops do not offer XMP; their memory speed is fixed by the manufacturer.
:::
::: variant fourDimms
With four DDR5 modules, AMD specifies DDR5-3600 for Ryzen 7000 and 9000 processors [4][5]. A speed below the kit's rating is therefore expected and not a missing setting.
:::

## How we detected it
We read every memory module from Windows (WMI): part number and the speed the memory currently runs at (`ConfiguredClockSpeed`). The rated speed comes from the part number, decoded with the catalog's tables for Corsair, G.Skill, Kingston, Crucial, TeamGroup and Patriot. `Win32_PhysicalMemory.Speed` is not used for this because it usually reports the standard speed. Unknown part numbers give "Unknown" and no advice. Four DDR5 modules on a Ryzen processor that run below the kit's rating give information instead of a problem, because of AMD's specification.

## How to fix
::: variant fourDimms
No action needed. If you want the kit's full speed, two modules are needed instead of four.
:::
::: variant default,laptop
1. **BitLocker:** before you change BIOS settings, check whether BitLocker or device encryption is on. If so, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update or a change to the TPM or the boot configuration can make Windows ask for it at the next start [7][8].
2. Restart into the BIOS: **Settings > System > Recovery > Advanced startup > Restart now**, then **Troubleshoot > Advanced options > UEFI Firmware Settings** (on some versions "UEFI Settings") [6]. Many PCs also open it when you press **Del** or **F2** during start.
::: if menuPath
3. On your {{board}}: **{{menuPath}}**.
::: if menuUnverified
   This path is not yet checked against the manual for your board. Menu names differ between boards and BIOS versions, so search for the setting by name if the path does not match.
:::
:::
::: ifnot menuPath
3. Look for the memory profile setting: **XMP**, **EXPO**, **DOCP** or **A-XMP**, usually on the overclocking or "Tweaker" page.
:::
4. Select the first profile, save and exit (usually **F10**).
5. The first start can take longer while the board trains the memory. If the PC does not start or crashes, update the BIOS first, or try the second profile. If it stays unstable, turn the profile off again: a stable PC at standard speed is better than an unstable fast one.
:::

## How to check the fix
Run the scan again. Current speed should equal the rated speed. Task Manager > Performance > Memory > **Speed** shows the same value.

## Sources
1. https://www.intel.com/content/www/us/en/gaming/extreme-memory-profile-xmp.html
2. https://www.amd.com/en/products/processors/technologies/expo.html
3. https://www.asus.com/support/faq/1042256/
4. https://www.amd.com/en/products/processors/desktops/ryzen/7000-series/amd-ryzen-7-7800x3d.html
5. https://www.amd.com/en/products/processors/desktops/ryzen/9000-series/amd-ryzen-7-9800x3d.html
6. https://support.microsoft.com/en-us/windows/experience/enable-virtualization-on-windows
7. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
8. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
