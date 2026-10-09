# Memory below its rated speed (XMP / EXPO)

## Summary
::: status Problem
Your memory is rated for {{rated}} MT/s but runs at {{configured}} MT/s. Turning on {{profileName}} in the BIOS lets it run at its rated speed.
:::
::: status Ok
Your memory runs at its rated speed of {{rated}} MT/s. {{profileName}} (or an equivalent setting) is active.
:::
::: status Unknown,Info,Unsupported
Checks whether the memory runs at the speed printed in its part number, which usually needs XMP or EXPO in the BIOS.
:::

## Why it matters
Memory modules start at a safe standard speed (JEDEC, for example DDR4-2133 or DDR5-4800). The faster speed on the box is stored as a profile (Intel XMP, AMD EXPO; ASUS calls it D.O.C.P on AM4) that must be switched on in the BIOS. Without it, the memory runs much slower than you paid for. Games that depend on memory bandwidth and latency lose frame rate and especially 1 % lows; on Ryzen processors the internal fabric clock is also tied to memory speed.
::: variant laptop
Most laptops do not offer XMP; their memory speed is fixed by the manufacturer.
:::

## How we detected it
We read every memory module from Windows (WMI): part number and the speed the memory currently runs at (`ConfiguredClockSpeed`). The rated speed comes from the part number, decoded with the catalog's tables for Corsair, G.Skill, Kingston, Crucial, TeamGroup and Patriot. `Win32_PhysicalMemory.Speed` is not used for this because it usually reports the standard speed. Unknown part numbers give "Unknown" and no advice.

## How to fix
1. Restart into the BIOS (press **Del** or **F2** during start).
::: if menuPath
2. On your {{board}}: **{{menuPath}}**.
:::
::: ifnot menuPath
2. Look for the memory profile setting: **XMP**, **EXPO**, **D.O.C.P** or **A-XMP**, usually on the overclocking or "Tweaker" page.
:::
3. Select the first profile, save and exit (usually **F10**).
4. The first start can take longer while the board trains the memory. If the PC does not start or crashes, update the BIOS first, or try the second profile.

## How to check the fix
Run the scan again. Current speed should equal the rated speed. Task Manager > Performance > Memory > **Speed** shows the same value.

## Sources
1. https://www.intel.com/content/www/us/en/gaming/extreme-memory-profile-xmp.html
2. https://www.amd.com/en/products/processors/technologies/expo.html
