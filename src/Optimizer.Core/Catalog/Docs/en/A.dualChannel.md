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
Desktop processors read memory through two channels at the same time. With modules in only one channel, memory bandwidth is cut in half. Games, and integrated graphics even more, become slower, often by 10 to 25 % in CPU-limited scenes, and 1 % lows suffer the most. Two modules in the wrong slots (both in channel A) are as slow as a single module.

## How we detected it
We read every module's slot names (`DeviceLocator` and `BankLabel`) from Windows and map them to channels with the catalog's patterns (for example, "ChannelA-DIMM2" means channel A). If the slot names do not reveal the channel, the result is "Unknown". Memory soldered on the mainboard can be dual channel internally and is not reported as a problem.

## How to fix
::: variant single
1. Add a second module of the same type, size and speed (ideally buy a matched kit of two).
2. Install it in the slot the board manual lists for two modules.
:::
::: variant sameChannel
1. Shut down the PC and unplug it.
2. Check the board manual ({{board}}) for the slots used with two modules. On most boards with four slots these are the second and fourth slot from the CPU (A2 and B2).
3. Move one module into the other channel.
:::
::: variant default
For two modules, use the slots the board manual recommends (usually A2 and B2).
:::

## How to check the fix
Run the scan again. The slots should show two different channels. Tools like CPU-Z show "Dual" as channel count.
