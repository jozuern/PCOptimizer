# MSI mode for this device (Expert)

## Summary
Lets the device use message-signaled interrupts instead of line-based ones. Many current drivers already do. Disputed, needs a restart.

## How it works
Devices signal the processor with interrupts. Older line-based interrupts can be shared between devices; message-signaled interrupts (MSI) are written to memory and never shared. Windows uses MSI when the driver requests it in the registry; the app sets that documented value (MSISupported = 1) for this device [1]. The change takes effect after a restart.

## Why it can help
If the device used shared line-based interrupts, MSI avoids other devices' interrupts being checked first, which can reduce delay spikes.

## Evidence
Most current graphics and network drivers already request MSI themselves; then this changes nothing. Benefits reported by users are not consistently measurable.

## Trade-offs & risks
A driver that does not handle MSI correctly can fail to start (Device Manager error), or in rare cases cause a blue screen. That is why it is an Expert, boot-critical change: create a restore point first. For a graphics card, Windows still starts with the basic display driver, and undo works from there.

## When not to use it
If the device already uses MSI, or for storage controllers (not offered here).

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/enabling-message-signaled-interrupts-in-the-registry
