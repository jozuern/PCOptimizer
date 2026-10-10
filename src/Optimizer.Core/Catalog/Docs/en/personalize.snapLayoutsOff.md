# Snap: no layouts on maximize button and screen top

## Summary
Hovering over the maximize button or dragging a window to the top of the screen no longer shows snap layouts.

## How it works
Settings has separate switches for snap layouts on the maximize button (the Snap flyout) and at the top of the screen (the Snap bar) [1]. The app sets EnableSnapAssistFlyout and EnableSnapBar to 0. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
No layout box pops up while you move or maximize windows.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
Layouts stay available with Windows + Z.

## When not to use it
If you arrange windows with the layouts.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/snap-your-windows
