# No "Restore previous versions" in the right-click menu

## Summary
Removes "Restore previous versions" from the classic right-click menu and the Previous Versions tab from Properties. Microsoft does not document the value; a VM test confirmed it.

## How it works
The entry opens older copies of a file or folder that File History, backups or restore points kept [1]. It comes from a shell extension, a small component that adds its own entries to the classic right-click menu (Show more options). Explorer skips shell extensions whose CLSID is listed as a value under HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked. The app adds {596AB062-B4D2-4215-9F74-E9109B0A8153}, the extension behind "Restore previous versions", to that list [1]. Microsoft does not document this list. A test in a virtual machine with Windows 11 confirmed it: "Restore previous versions" was gone from the classic menu of a folder and of a video file and came back after the value was removed. In the VM test, menus opened after the change already left the entry out, without an Explorer restart. If one still shows it, sign out and in again [1].

## Why it can help
If you never restore older copies this way, the entry is clutter in every menu.

## Evidence
No performance effect; only the menu changes. Your backups, File History and restore points stay as they are.

## Trade-offs & risks
The same extension draws the Previous Versions tab in Properties (registered for files, folders and drives): in the VM test that tab was gone from the Properties of a folder and of a video file too, and back after Undo. Older copies then come back only through File History or your backup program itself. Because the value is undocumented, a future Windows version may ignore it; Undo removes it either way.

## When not to use it
If you use File History or backups and restore older copies from this menu.

## Sources
1. https://winaero.com/remove-previous-versions-windows-10/
