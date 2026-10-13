# No "Give access to" in the right-click menu

## Summary
Removes "Give access to" from the classic right-click menu and the Sharing tab from the Properties of folders and drives. Microsoft does not document the value; a VM test confirmed it.

## How it works
Explorer builds the classic right-click menu (Show more options) partly from shell extensions, small components that add their own entries. Explorer skips every shell extension whose CLSID is listed as a value under HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked. The app adds {f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}, the extension behind "Give access to", to that list [1]. Microsoft does not document this list. A test in a virtual machine with Windows 11 confirmed it: "Give access to" was gone from the classic menu of a folder and came back after the value was removed. Menus opened after the change already reflected it, without an Explorer restart; if one still shows the entry, sign out and in again [1].

## Why it can help
On a PC that never shares folders on a network, the entry is clutter in every folder menu.

## Evidence
No performance effect; only the menu changes.

## Trade-offs & risks
The same extension draws the Sharing tab in the Properties of folders and drives: in the VM test that tab was gone too, and back after Undo. Sharing a folder from Explorer then needs Undo first. Folders already shared stay shared. Because the value is undocumented, a future Windows version may ignore it; Undo removes it either way.

## When not to use it
If you share folders with other people on your network through this menu or the Sharing tab.

## Sources
1. https://www.majorgeeks.com/content/page/remove_the_give_access_to_context_menu_in_windows_10.html
