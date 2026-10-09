# PCIe power saving on battery: maximum

## Summary
Sets PCIe link power management on battery to Maximum power savings, the Windows default for the Balanced plan. Only changes something if a tool or the manufacturer lowered it.

## How it works
Active State Power Management (ASPM) lets PCIe links (SSD, Wi-Fi card, graphics) drop into low-power states when idle [1]. The power plan has separate values for mains and battery; the app sets the battery value to 2 (Maximum power savings).

## Why it can help
Idle links that cannot sleep keep drawing power. Laptops spend most of their battery time with idle links, so restoring maximum savings lowers idle consumption.

## Evidence
Windows itself uses Maximum power savings on battery in the Balanced plan. The app only recommends this when your current value differs from that default.

## Trade-offs & risks
Waking a link takes microseconds. Some older devices had stability problems with ASPM; if a device misbehaves on battery afterwards, use Undo.

## When not to use it
If a PCIe device (for example an external dock or a capture card) drops out on battery with this setting.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/pci-express-settings-link-state-power-management
