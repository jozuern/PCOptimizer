# Gaming power plan (based on High performance)

## Summary
Creates the plan "PCOptimizer Gaming" as a copy of High performance and activates it. Clocks stay higher between bursts of load.

## How it works
The app copies the built-in High performance plan, names the copy and makes it active. High performance keeps a higher minimum processor state and parks cores less aggressively than Balanced [1]. Your other plans are not changed.

## Why it can help
In games with uneven load, the processor does not have to ramp up its clock first at each burst, which can smooth frame times slightly.

## Evidence
Compared with Balanced on a current desktop, measured differences are usually small, often within run-to-run variance. Coming from Power saver the gain is large.

## Trade-offs & risks
Higher idle power and temperature. Not offered on Ryzen X3D processors with two chiplets (they need Balanced) or on laptops with Modern Standby.

## When not to use it
Do not use on multi-chiplet Ryzen X3D CPUs or on laptops. If you already use Ultimate Performance, keep one of the two.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings
