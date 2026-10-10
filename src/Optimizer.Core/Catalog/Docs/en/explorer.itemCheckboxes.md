# File Explorer: item check boxes

## Summary
Shows a check box next to files and folders for selecting several items, like View > Show > Item check boxes.

## How it works
Item check boxes show a box next to files and folders [1]. The app sets AutoCheckSelect to 1. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
Selecting several files works without holding Ctrl.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
A little more clutter next to file names.

## When not to use it
If you select files with Ctrl and Shift.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/fileexplorer/file-explorer-in-windows
