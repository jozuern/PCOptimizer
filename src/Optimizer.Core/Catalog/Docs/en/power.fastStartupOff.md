# Fast Startup off

## Summary
Makes "Shut down" a real shutdown again. Drivers start fresh on the next boot instead of being restored from a hibernation file.

## How it works
With Fast Startup, shutting down saves the kernel and drivers to the hibernation file and restores them at the next start [1]. Without it, Windows initializes all drivers from scratch on every boot.

## Why it can help
Problems that build up in a driver (for example after a GPU driver update) are cleared by a normal shutdown, not only by Restart. Dual boot and BIOS changes also behave more predictably.

## Evidence
No frame-rate effect. Boot takes a few seconds longer on most PCs with an SSD.

## Trade-offs & risks
Slightly slower cold boot.

## When not to use it
Not needed if you rarely shut down, or if boot time matters more to you than a clean driver state.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/distinguishing-fast-startup-from-wake-from-hibernation
