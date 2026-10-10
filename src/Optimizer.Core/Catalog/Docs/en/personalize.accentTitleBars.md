# Accent color on title bars

## Summary
Shows your accent color on window title bars and borders, like the switch in Settings > Personalization > Colors.

## How it works
The option "Show accent color on title bars and window borders" colors the bar at the top of each window and its borders [1]. The app sets ColorPrevalence to 1. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
The active window is easier to tell apart.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
None beyond the look.

## When not to use it
If you prefer neutral title bars.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/personalize-your-colors-in-windows
