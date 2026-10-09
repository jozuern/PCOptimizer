# Network adapter power saving off

## Summary
Turns off the power saving features of wired network adapters (Energy Efficient Ethernet, Green Ethernet and similar). Restarts the adapter.

## How it works
Many Ethernet drivers put the link into a low power idle state between packets (Energy Efficient Ethernet, IEEE 802.3az) or lower power on short cables [1]. The app sets each of these settings to off, but only those that your adapter's driver actually offers, using the values the driver lists. The adapter restarts so the driver reads the new values; the connection drops for a few seconds. Undo restores every value.

## Why it can help
Waking the link from low power idle takes a few microseconds per wake. More important in practice: some adapter and switch combinations drop the link or lose packets with Energy Efficient Ethernet on.

## Evidence
The delay itself is too small to notice in games. The benefit is fewer disconnects on hardware with such problems.

## Trade-offs & risks
Slightly higher power use (well below one watt per adapter). The adapter restarts once when applying and once on undo.

## When not to use it
If your connection is stable and power use matters, for example on a laptop.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/enumeration-keywords
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/standardized-inf-keywords-for-power-management
