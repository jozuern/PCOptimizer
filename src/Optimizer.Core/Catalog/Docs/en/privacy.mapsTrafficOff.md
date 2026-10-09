# Offline maps updates off

## Summary
Stops Windows from downloading and updating offline maps in the background.

## How it works
Two policies turn off automatic map downloads and the network traffic of the offline maps settings page [1][2].

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Offline maps you downloaded are no longer updated automatically. The Windows Maps app is deprecated and was removed from the Microsoft Store in July 2025 [3], so on most PCs this changes little.

## When not to use it
If you use offline maps.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-maps
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
3. https://learn.microsoft.com/en-us/windows/whats-new/deprecated-features
