# Switch to the Balanced power plan

## Summary
Switches to Balanced, the Windows default: instead of Power saver, the CPU reaches full clock again; in the Quiet and cool and Battery profiles, it replaces High performance for less heat and noise.

## How it works
Windows power plans set how the processor raises and lowers its clock, when it parks cores and how devices save power. Power saver caps processor performance and reacts slowly to load. Balanced raises the clock quickly when there is work and lowers it at idle [1]. High performance, and plans based on it such as Ultimate Performance, deliver maximum performance at the expense of higher power consumption; Balanced matches performance and power consumption to demand [2].

## Why it can help
A game on a CPU held back by Power saver gets fewer frames and less even frame times. Balanced removes the cap without raising idle power much.

In the Quiet and cool and Battery profiles it is the other way round: a High performance plan uses more power than needed at light load, which means more heat, more fan noise and less battery time. Balanced lowers power when there is little to do and still reaches the full clock under load [2].

## Evidence
Coming from Power saver, the effect depends on how strongly the plan limited the CPU; on desktops it is usually large. Coming from High performance, the difference is power and heat, not frame rate, and its size depends on the processor. Balanced is also the plan AMD recommends for Ryzen X3D processors with two chiplets.

## Trade-offs & risks
Slightly higher power draw under load than Power saver. None at idle compared with Power saver on most desktops. Compared with High performance there is no lower clock limit under load.

## When not to use it
For gaming it is not needed if you already use Balanced, High performance or a custom performance plan. That is why the app recommends it over High performance only in the Quiet and cool and Battery profiles.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings
2. https://learn.microsoft.com/en-us/windows/win32/power/power-policy-settings
