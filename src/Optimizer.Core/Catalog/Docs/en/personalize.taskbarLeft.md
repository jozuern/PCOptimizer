# Taskbar: icons on the left

## Summary
Moves Start and the taskbar icons to the left, like Settings > Personalization > Taskbar > Taskbar behaviors > Taskbar alignment.

## How it works
Taskbar icons are centered by default and can be aligned to the left [1]. The app sets TaskbarAl to 0. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
Start stays in the same corner, also on wide monitors.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
None beyond the look.

## When not to use it
If you like the centered taskbar.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
