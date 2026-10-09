# PCIe link power management off

## Summary
Turns off PCIe link power management (ASPM) on mains power, so links stay in full power at idle. A gaming benefit is disputed; it mainly raises idle power.

## How it works
ASPM lets PCIe links (graphics card, NVMe SSD, network card) enter low-power states when idle; Moderate power savings uses a light state, Maximum power savings a deeper one [1]. Leaving a low-power state adds a short delay. In the Windows defaults, Balanced uses Moderate power savings on mains power and High performance uses Off. The app sets the mains value to Off.

## Why it can help
It removes the wake-up delay of idle links.

## Evidence
Gaming measurements rarely show a difference. GPUs under load do not idle their link in the first place.

## Trade-offs & risks
Slightly higher idle power, mostly on systems with several PCIe devices.

## When not to use it
Not needed without a specific problem. Leave it on laptops for battery life.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/pci-express-settings-link-state-power-management
