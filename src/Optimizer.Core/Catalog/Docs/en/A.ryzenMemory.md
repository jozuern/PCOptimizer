# Ryzen memory speed

## Summary
::: variant aboveSync
Memory runs at {{speed}} MT/s, above the range where the Ryzen memory controller usually runs in sync. Check that it did not switch to half speed.
:::
::: variant slowKit
Memory runs at {{speed}} MT/s, below the usual range for Ryzen ({{low}} to {{high}} MT/s). A faster kit improves minimum frame rates.
:::
::: variant default
Checks whether the memory speed fits the Ryzen memory controller.
:::

## Why it matters
On Ryzen, the memory controller and the connection between the processor's dies run at clocks tied to the memory clock. Games react strongly to memory latency, especially minimum frame rates.
::: if am4
On AM4, the Infinity Fabric clock (FCLK) can usually follow the memory clock 1:1 up to about DDR4-3600, on many processors up to DDR4-3800. Above that it switches to a 2:1 ratio, and the extra memory speed often costs more latency than it gains.
:::
::: if am5
On AM5, the memory controller clock (UCLK) runs at the memory clock by default up to about DDR5-6000. Above that the BIOS usually switches it to half speed (1:2), which adds latency unless you set 1:1 manually and the processor handles it.
:::

## How we detected it
We read the configured memory speed and the rated speed from the module part numbers. The actual FCLK or UCLK ratio cannot be read by Windows, so this is guidance, not a measurement.

## How to fix
::: variant aboveSync
::: if am4
1. In the BIOS, check that **FCLK** is set to half the memory speed (for DDR4-3600: 1800 MHz). If it cannot run that high, DDR4-3600 with 1:1 is usually as fast or faster.
:::
::: if am5
1. In the BIOS, look for **UCLK DIV1 MODE** (wording varies) and set **UCLK = MEMCLK**. If the system is not stable that way, DDR5-6000 with 1:1 is usually as fast or faster.
:::
:::
::: variant slowKit
1. For an upgrade, a kit in the range {{low}} to {{high}} MT/s with low CL timings fits this platform. Use the same number of modules in the slots the manual recommends for two modules.
:::
::: variant default
No action needed.
:::

## How to check the fix
Run the scan again. For the clock ratio, tools such as ZenTimings or the BIOS show FCLK, UCLK and MCLK.
