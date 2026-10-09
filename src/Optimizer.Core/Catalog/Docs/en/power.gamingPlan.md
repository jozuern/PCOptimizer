# Gaming power plan (based on High performance)

## Summary
Creates the plan "PCOptimizer Gaming" as a copy of High performance and activates it. Clocks stay higher between bursts of load.

## How it works
The app copies the built-in High performance plan, names the copy and makes it active. Microsoft describes High performance as delivering maximum performance at the expense of higher power consumption [1]. On mains power, its Windows defaults keep the minimum processor state at 100 % instead of the 5 % of Balanced, so the clock stays high even at light load [2]. Your other plans are not changed.

## Why it can help
In games with uneven load, the processor does not have to ramp up its clock first at each burst, which can smooth frame times slightly.

## Evidence
Compared with Balanced on a current desktop, differences are usually small, often within run-to-run variance. Coming from Power saver, the gain depends on the processor and the game.

## Trade-offs & risks
Higher idle power and temperature. The Windows power mode setting is only available with Balanced or plans derived from it, so it disappears while this plan is active [3]. Not offered on laptops, on PCs with Modern Standby (they only allow Balanced [1]) or on Ryzen X3D processors with two chiplets.

## When not to use it
Do not use on multi-chiplet Ryzen X3D CPUs. If you already use Ultimate Performance, keep one of the two.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/power/power-policy-settings
2. https://learn.microsoft.com/en-us/windows-server/administration/performance-tuning/hardware/power/power-performance-tuning
3. https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/customize-power-slider
