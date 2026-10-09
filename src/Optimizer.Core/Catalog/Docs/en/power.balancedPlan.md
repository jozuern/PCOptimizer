# Switch to the Balanced power plan

## Summary
Replaces the Power saver plan with Balanced, the Windows default. The processor can reach its full clock again.

## How it works
Windows power plans set how the processor raises and lowers its clock, when it parks cores and how devices save power. Power saver caps processor performance and reacts slowly to load. Balanced raises the clock quickly when there is work and lowers it at idle [1].

## Why it can help
A game on a CPU held back by Power saver gets fewer frames and less even frame times. Balanced removes the cap without raising idle power much.

## Evidence
The effect depends on how strongly the saver plan limited the CPU; on desktops it is usually large. Balanced is also the plan AMD recommends for Ryzen X3D processors with two chiplets.

## Trade-offs & risks
Slightly higher power draw under load than Power saver. None at idle compared with Power saver on most desktops.

## When not to use it
Not needed if you already use Balanced, High performance or a custom performance plan.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings
