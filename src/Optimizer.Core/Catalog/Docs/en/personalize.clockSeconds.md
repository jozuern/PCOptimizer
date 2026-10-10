# Taskbar clock with seconds

## Summary
Shows seconds in the taskbar clock. Microsoft notes that this uses more power.

## How it works
Settings offers "Show seconds in system tray clock" and notes that it uses more power [1]. The app sets ShowSecondsInSystemClock to 1. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
Useful for timing things, for example when a match or a sale starts.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
A little more power use, which matters on battery [1].

## When not to use it
On a laptop where battery life matters most.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
