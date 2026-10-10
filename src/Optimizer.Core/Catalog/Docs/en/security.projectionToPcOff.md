# No wireless projection to this PC

## Summary
Other devices can no longer project their screen to this PC with Miracast, and the option cannot be turned on. Pro, Enterprise and Education.

## How it works
With the policy AllowProjectionToPC = 0, projection to this PC is not allowed: always off, and users cannot enable it [1]. By default the PC can be projected to from the lock screen [1].

## Why it can help
The PC does not announce itself as a wireless display to nearby devices.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
You cannot use this PC as a wireless display for a phone or another PC.

## When not to use it
If you project a phone or laptop to this PC.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-wirelessdisplay
