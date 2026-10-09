# Hardware-accelerated GPU scheduling (HAGS)

## Summary
Lets the graphics card schedule its own work instead of Windows doing it on the CPU. Required for DLSS Frame Generation.

## How it works
Since WDDM 2.7, Windows can hand most GPU scheduling to a dedicated processor on the graphics card instead of a high-priority CPU thread [1]. Only supporting GPUs and drivers use it. The value HwSchMode = 2 turns it on; it takes effect after a restart.

## Why it can help
It removes some scheduling work from the CPU and can lower latency slightly. On NVIDIA RTX 40 and 50 cards it is required for DLSS Frame Generation.

## Evidence
For normal rendering, independent tests mostly show differences within run-to-run variance. The Frame Generation requirement is documented by NVIDIA and Microsoft.

## Trade-offs & risks
Rare problems with older capture or overlay software. If stutter or black screens appear, turn it off again (on Frame Generation GPUs that disables Frame Generation).

## When not to use it
On cards with Frame Generation it should stay on. On older cards, use it only if you see no problems.

## Sources
1. https://devblogs.microsoft.com/directx/hardware-accelerated-gpu-scheduling/
