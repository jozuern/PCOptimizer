# No recently opened files in Start, jump lists and File Explorer

## Summary
Turns off the setting "Show recently opened items in Start, Jump Lists, and File Explorer", so no recent files appear there.

## How it works
Microsoft names Start_TrackDocs = 0 under HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced as the registry way to turn off the recommended files in Start; it is the switch "Show recently opened items in Start, Jump Lists, and File Explorer" in Settings > Personalization > Start [1].

## Why it can help
People who share or watch your screen do not see which files you opened recently.

## Evidence
Documented by Microsoft [1]. The list is stored on this PC, so this is about what others see. A privacy setting without effect on frame rate or latency.

## Trade-offs & risks
No quick access to recent files from Start, taskbar jump lists or the File Explorer home page.

## When not to use it
If you reopen files from the recent lists.

## Sources
1. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
