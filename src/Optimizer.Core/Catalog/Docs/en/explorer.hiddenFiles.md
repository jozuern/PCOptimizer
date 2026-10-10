# File Explorer: show hidden files

## Summary
Shows hidden files and folders, such as AppData, like View > Show > Hidden items in File Explorer. Protected system files stay hidden.

## How it works
File Explorer shows hidden items when you select View > Show > Hidden items [1]. The app sets Hidden to 1. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
Game saves, mods and configuration files often live in hidden folders such as AppData.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
More items in folders and on the desktop, which you can delete by mistake.

## When not to use it
If other people use this PC and should not see hidden folders.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/fileexplorer/file-explorer-in-windows
