# MSI mode for this device (Expert)

## Summary
Lets the device use message-signaled interrupts instead of line-based ones. Disputed: user reports of a benefit are not consistently measurable. Needs a restart.

## How it works
Devices signal the processor with interrupts. Older line-based interrupts can be shared between devices; message-signaled interrupts (MSI) are written to memory and are not shared. Windows uses MSI for a device when its registry entry MSISupported is 1; normally the driver's installer sets it [1]. The app sets this value for this device. The change takes effect after a restart.

## Why it can help
If the device used shared line-based interrupts, MSI avoids other devices' interrupts being checked first, which can reduce delay spikes.

## Evidence
If MSISupported is already 1 for this device, the item shows as on and nothing changes. Benefits reported by users are not consistently measurable.

## Trade-offs & risks
If the driver's installer set MSISupported to 0, the vendor chose line-based interrupts on purpose, and forcing MSI can make the driver fail to start (Device Manager error) or, in rare cases, crash Windows. A driver update can also write its own value again. That is why this is an Expert, boot-critical change; the app creates a restore point first.

## When not to use it
If the device already uses MSI, or for storage controllers (not offered here).

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/enabling-message-signaled-interrupts-in-the-registry
