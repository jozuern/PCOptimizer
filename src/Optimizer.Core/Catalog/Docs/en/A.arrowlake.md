# Intel Core Ultra 200S: BIOS update with microcode {{min}}

## Summary
::: status Problem
Your BIOS provides microcode older than {{min}}. Intel's later updates for Core Ultra 200S fix performance issues in games.
:::
::: status Ok
Your BIOS provides microcode {{min}} or newer with Intel's game performance fixes for Core Ultra 200S.
:::
::: status Unknown,Info,Unsupported
Checks that Core Ultra 200S desktop CPUs run BIOS microcode with Intel's game performance fixes.
:::

## Why it matters
At launch, Intel Core Ultra 200S (Arrow Lake) processors performed below expectations in many games. Intel traced part of this to firmware and released microcode {{min}} together with an updated CSME firmware kit (19.0.0.1854v2.2 or newer) and Windows updates. Boards with older BIOS versions miss these fixes. The gain depends on the game and is usually a few percent.

## How we detected it
We read the processor model and the microcode revision loaded by the BIOS. Model list and minimum revision come from the catalog.

## How to fix
1. Open the support page for your board ({{board}}).
2. Install the newest BIOS that lists microcode {{min}} and CSME firmware 19.0.0.1854v2.2 or newer, following the vendor's instructions.
3. Install the latest Windows updates and the Intel chipset and PPM drivers from the board vendor.

## How to check the fix
Run the scan again. The microcode from the BIOS should be {{min}} or newer.

## Sources
1. https://www.elevenforum.com/t/field-update-1-of-2-intel-core-ultra-200s-series-performance-status.31640/latest
