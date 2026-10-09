# Memory modules from different kits

## Summary
::: status Info
The memory modules have different part numbers or sizes, so they are not one matched kit. Mixed modules may not run their XMP or EXPO speed stably.
:::
::: status Ok,Unknown,Unsupported,Problem
Checks that all memory modules are the same model and size, as in one matched kit.
:::

## Why it matters
Memory kits are validated for their rated speed only with the modules in that kit. Combining kits, even of the same speed, risks stability problems, XMP or EXPO not enabling, or a PC that does not boot [1]. Board makers recommend one matched set for two or four modules [2]. Unstable memory shows up as crashes and errors; memory that has to run below its profile costs frame rate.

## How we detected it
We read the part number and size of every module from Windows (`Win32_PhysicalMemory`). Different part numbers or sizes mean different kits. Two kits of the same model look identical here, so this check cannot detect them.

## How to fix
1. If the PC is stable and the memory runs at its rated speed (see the memory speed check), nothing needs to change.
2. If the PC crashes or the XMP or EXPO profile does not hold, test with the profile off first, then raise the speed again step by step.
3. For a lasting fix, use one matched kit of the total size you need, ideally from the board's memory support list (QVL) [2].

## How to check the fix
Run the scan again. All modules should show the same part number and size.

## Sources
1. https://www.corsair.com/us/en/explorer/diy-builder/memory/can-i-mix-corsair-memory-kits/
2. https://www.asus.com/support/faq/1042256/
