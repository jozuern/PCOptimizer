# File Explorer: full path in the title

## Summary
Shows the full folder path, such as C:\Users\Name\Documents, in the File Explorer title instead of only the folder name.

## How it works
The Folder Options box "Display the full path in the title bar" shows the full path of the open folder [1]. The app sets FullPath to 1. The tutorial restarts File Explorer after setting the value [1], so the change shows after Explorer restarts or you sign out. Microsoft does not document the registry value behind the switch; the tutorial [1] shows the value the switch writes. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
You see where you are, for example to tell game folders with the same name apart.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
None beyond the look.

## When not to use it
If you prefer short titles.

## Sources
1. https://www.elevenforum.com/t/turn-on-or-off-display-full-path-in-title-bar-of-file-explorer-in-windows-11.3585/
