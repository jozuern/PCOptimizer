# Memory in single channel

## Summary
::: variant single
Only one memory module is installed, so the memory runs in single channel with half the bandwidth.
:::
::: variant sameChannel
All memory modules sit in the same channel, so the memory runs in single channel with half the bandwidth.
:::
::: variant default
Checks that the memory runs in dual channel. That needs modules in both channels.
:::

## Why it matters
Desktop processors read memory through two channels at the same time. With modules in only one channel, peak memory bandwidth is cut in half [1]. Games that depend on memory bandwidth, and integrated graphics even more, become slower, and 1 % lows usually suffer first. Two modules in the wrong slots (both in channel A) are as slow as a single module.

## How we detected it
We read every module's slot names (`DeviceLocator` and `BankLabel`) from Windows and map them to channels with the catalog's patterns (for example, "ChannelA-DIMM2" means channel A). If the slot names do not reveal the channel, the result is "Unknown". A single module counts as single channel only when Windows reports it as a regular DIMM or SODIMM. Anything else gives "Unknown", because memory soldered on the mainboard can run in dual channel internally.

## How to fix
::: variant single
1. Add a second module of the same type, size and speed (ideally buy a matched kit of two) [1].
2. Install it in the slot the board manual lists for two modules.
:::
::: variant sameChannel
1. Shut down the PC and unplug it.
2. Check the board manual ({{board}}) for the slots used with two modules. ASUS, for example, uses DIMM_A2 and DIMM_B2 [2], which on most boards with four slots are the second and fourth slot from the CPU.
3. Move one module into the other channel.
:::
::: variant default
For two modules, use the slots the board manual recommends (often A2 and B2) [2].
:::

## How to check the fix
Run the scan again. The slots should show two different channels. Tools like CPU-Z show "Dual" as channel count.

## Sources
1. https://www.corsair.com/us/en/explorer/diy-builder/memory/what-is-dual-channel-ram/
2. https://www.asus.com/support/faq/1047257/
