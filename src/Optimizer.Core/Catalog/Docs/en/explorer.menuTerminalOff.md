# No "Open in Terminal" in the right-click menu

## Summary
Removes "Open in Terminal" from the right-click menu of folders, in the Windows 11 menu and the classic one. Terminal stays installed. Microsoft does not document the value; a VM test confirmed it.

## How it works
Windows Terminal, which comes with Windows 11, adds "Open in Terminal" to the right-click menu of folders through a shell extension [1]. Explorer skips every shell extension whose CLSID is listed as a value under HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked. The app adds {9F156763-7844-4DC4-B2B1-901F640F5155}, the extension behind "Open in Terminal", to that list [1]. Microsoft does not document this list. A test in a virtual machine with Windows 11 confirmed it: "Open in Terminal" was gone from the Windows 11 menu and the classic menu of a folder and came back after the value was removed. Menus opened after the change already reflected it, without an Explorer restart; if one still shows the entry, sign out and in again [1].

## Why it can help
If you rarely use a command line, the entry takes a line in every folder menu.

## Evidence
No performance effect; only the menu changes.

## Trade-offs & risks
Terminal itself stays installed and starts from Start as before. Because the value is undocumented, a future Windows version may ignore it; Undo removes it either way.

## When not to use it
If you open a command line in a folder through this entry.

## Sources
1. https://winaero.com/how-to-remove-open-in-windows-terminal-context-menu-in-windows-11/
