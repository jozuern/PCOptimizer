# Ultimate Performance power plan

## Summary
Creates and activates a copy of the hidden Ultimate Performance plan. It removes the remaining fine-grained power saving of High performance.

## How it works
Windows contains a hidden plan for workstations, Ultimate Performance. It disables most remaining power-saving timers on top of High performance [1]. The app duplicates it under the name "PCOptimizer Ultimate" and activates the copy.

## Why it can help
It can shave off small wake-up delays of the processor and devices. In theory that helps latency-sensitive tasks.

## Evidence
Gaming tests rarely show a difference to High performance beyond run-to-run variance. Treat it as disputed.

## Trade-offs & risks
Highest idle power and heat of all plans. Not offered on Ryzen X3D processors with two chiplets or on laptops.

## When not to use it
Do not use on multi-chiplet Ryzen X3D CPUs, on laptops, or if quiet and cool idle operation matters to you.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings
