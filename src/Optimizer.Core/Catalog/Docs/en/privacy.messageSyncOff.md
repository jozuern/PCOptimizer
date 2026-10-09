# Text message cloud backup off

## Summary
Stops text messages on this device from being backed up to the cloud.

## How it works
The policy "Allow Message Service Cloud Sync" is set to off [1].

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Only relevant for devices with a cellular modem.

## When not to use it
If you use SMS on this device and want a backup.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-messaging
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
