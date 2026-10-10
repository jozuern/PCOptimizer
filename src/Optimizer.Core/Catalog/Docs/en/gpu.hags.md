# Hardware-accelerated GPU scheduling (HAGS)

## Summary
Lets the graphics card schedule its own work instead of a CPU thread. Required for DLSS Frame Generation; without Frame Generation no measurable gain is documented.

## How it works
Since WDDM 2.7, Windows can hand most GPU scheduling to a dedicated processor on the graphics card instead of a high-priority CPU thread [1]. Only supporting GPUs and drivers use it. The value HwSchMode = 2 turns it on; it takes effect after a restart. It is the value behind the switch "Hardware-accelerated GPU scheduling" in Settings > System > Display > Graphics; Microsoft does not document the value on its own.

## Why it can help
On NVIDIA RTX 40 and 50 cards, DLSS Frame Generation only works with this setting on [2]. Without Frame Generation, Microsoft describes the change as one users should not notice [1].

## Evidence
Microsoft introduced the setting in 2020 as an opt-in option and expects no significant visible change [1]. NVIDIA's Frame Generation integration guide lists it as a requirement: if it is off, Frame Generation is unavailable [2]. We found no vendor measurement of a gain in normal rendering.

## Trade-offs & risks
If stutter, black screens or problems with capture or overlay software appear after the change, turn it off again (on Frame Generation GPUs that disables Frame Generation).

## When not to use it
On cards with Frame Generation it should stay on. On other cards there is nothing documented to gain. Intel Arc A-Series cards do not support it [3].

## Sources
1. https://devblogs.microsoft.com/directx/hardware-accelerated-gpu-scheduling/
2. https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuideDLSS_G.md
3. https://www.intel.com/content/www/us/en/support/articles/000056788/graphics.html
