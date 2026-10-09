# Application compatibility telemetry off

## Summary
Stops the application telemetry and the program inventory that report installed apps and their use.

## How it works
The policies "Turn off Application Telemetry" and "Turn off Inventory Collector" are set [1]. Turning off the Inventory Collector also stops the Program Compatibility Assistant from collecting installation data [1]. A restart completes the change.

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Microsoft gets less data to detect app compatibility problems before feature updates.

## When not to use it
On PCs where feature update compatibility matters more than privacy.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-appcompat
