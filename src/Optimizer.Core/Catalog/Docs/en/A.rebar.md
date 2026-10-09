# Resizable BAR

## Summary
::: variant off
{{gpu}} supports Resizable BAR, but it is off. Turn on Above 4G Decoding and Resizable BAR in the BIOS.
:::
::: variant active
Resizable BAR is active for {{gpu}}: the processor can access all of the video memory at once.
:::
::: variant unsupported
{{gpu}} does not support Resizable BAR. Nothing to change. No BIOS setting can add it.
:::
::: variant unknownGpu,unknownState
Checks whether Resizable BAR (AMD: Smart Access Memory) is active for the graphics card.
:::

## Why it matters
Without Resizable BAR, the processor sees the video memory only through a 256 MB window and has to move that window around to reach the rest. With it, the whole video memory is mapped at once. In many games that changes little, in some it adds a few percent, and for Intel Arc cards it is essential: without it they lose a large part of their performance. Support needs a recent graphics card (NVIDIA RTX 30 series and newer, AMD RX 6000 and newer, Intel Arc), UEFI boot and a BIOS setting.
::: variant unsupported
Older cards like the GeForce RTX 20 series do not support it, so the setting would have no effect.
:::

## How we detected it
We read the size of the graphics card's memory windows (PCI memory resources) through the Windows configuration manager. A window larger than 256 MB means Resizable BAR is active. Whether the card supports it comes from the catalog's GPU table. We also check UEFI boot and the system disk's partition style.

## How to fix
::: variant off
::: if mbr
0. Your system disk uses MBR. Turning off CSM (legacy boot) would make Windows unbootable. Convert the disk to GPT first with `mbr2gpt /validate` and `mbr2gpt /convert` (back up first).
:::
1. Update the BIOS to a version with Resizable BAR support.
::: if menuPath
2. On your {{board}}: **{{menuPath}}**.
:::
::: ifnot menuPath
2. Enable **Above 4G Decoding** and **Re-Size BAR Support** (ASRock: C.A.M.).
:::
3. Make sure **CSM** is disabled (pure UEFI boot).
4. Update the graphics driver.
:::
::: variant active,unsupported,unknownGpu,unknownState
No action needed.
:::

## How to check the fix
Run the scan again. The largest memory window should match the video memory size. NVIDIA Control Panel > System Information also shows "Resizable BAR: Yes".

## Sources
1. https://www.nvidia.com/en-us/geforce/news/geforce-rtx-30-series-resizable-bar-support/
2. https://www.intel.com/content/www/us/en/support/articles/000090831/graphics.html
