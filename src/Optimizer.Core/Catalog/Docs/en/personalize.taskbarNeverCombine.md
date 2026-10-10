# Taskbar: never combine buttons

## Summary
Shows every window as its own labeled taskbar button, on the main taskbar and on other displays.

## How it works
"Combine taskbar buttons" offers always (the default), when the taskbar is full, and never, with a separate choice for other displays [1]. The app sets both TaskbarGlomLevel and MMTaskbarGlomLevel to 2 (never). Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
You switch to the right window with one click instead of picking it from a group.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
With many windows the buttons get small and the taskbar scrolls [1].

## When not to use it
If you often have many windows open.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
