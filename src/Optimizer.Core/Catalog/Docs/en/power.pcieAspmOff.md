# PCIe link power management off

## Summary
Turns off Active State Power Management (ASPM) for PCIe links on mains power. Links no longer drop into low-power states at idle.

## How it works
ASPM lets PCIe links (graphics card, NVMe SSD, network card) enter low-power states when idle [1]. Returning to full power takes microseconds. The app sets the plan value for mains power to Off.

## Why it can help
Removes the wake-up delay of idle links. Some boards also had stability problems with ASPM, which this avoids.

## Evidence
Gaming measurements rarely show a difference. GPUs under load do not idle their link in the first place.

## Trade-offs & risks
Slightly higher idle power, mostly on systems with several PCIe devices.

## When not to use it
Not needed without a specific problem. Leave it on laptops for battery life.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/pci-express-settings-link-state-power-management
