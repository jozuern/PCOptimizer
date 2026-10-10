# File Explorer: compact view

## Summary
Reduces the space between files in File Explorer, like View > Compact view.

## How it works
Compact view reduces the space between files [1]. The app sets UseCompactMode to 1. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
More files fit on the screen, which helps with mouse and keyboard.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
Smaller targets on touch screens.

## When not to use it
On a touch screen.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/fileexplorer/file-explorer-in-windows
