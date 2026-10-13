# Drive letters first

## Summary
This PC and the navigation pane show "(C:) Local Disk" instead of "Local Disk (C:)". Microsoft does not document the value; a VM test confirmed it.

## How it works
Explorer names drives with their label first and the letter in brackets after it. The value ShowDriveLettersFirst under HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer set to 4 puts the letter in front for all drives [1]. Microsoft does not document this value. A test in a virtual machine with Windows 11 confirmed it: after Explorer restarted, This PC showed "(C:) Local Disk", and "Local Disk (C:)" again after the value was removed. Explorer picks the change up when it starts again: use Restart Explorer on the Tools page, or sign out and in.

## Why it can help
With several drives, the letters line up at the start of each name and are quicker to find.

## Evidence
No performance effect; only the order of letter and label changes.

## Trade-offs & risks
None beyond the changed look. Because the value is undocumented, a future Windows version may ignore it; Undo removes it either way.

## When not to use it
If you prefer the label first.

## Sources
1. https://winaero.com/show-drive-letters-before-drive-names-in-this-pc-computer-folder/
