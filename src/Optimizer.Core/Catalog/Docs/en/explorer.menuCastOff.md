# No "Cast to Device" in the right-click menu

## Summary
Removes "Cast to Device" from the classic right-click menu of videos and pictures. Microsoft does not document the value; a VM test confirmed it.

## How it works
"Cast to Device" sends a video or picture to another device on the network over DLNA [1]. It comes from a shell extension, a small component that adds its own entries to the classic right-click menu (Show more options). Explorer skips shell extensions whose CLSID is listed as a value under HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked. The app adds {7AD84985-87B4-4a16-BE58-8B72A5B390F7}, the extension behind "Cast to Device", to that list [1]. Microsoft does not document this list. A test in a virtual machine with Windows 11 confirmed it: "Cast to Device" was gone from the classic menu of a video file and came back after the value was removed. In the VM test, menus opened after the change already left the entry out, without an Explorer restart. If one still shows it, sign out and in again [1].

## Why it can help
Without a DLNA device on the network, the entry is clutter in every media file menu.

## Evidence
No performance effect; only the menu changes.

## Trade-offs & risks
You can no longer start casting from the right-click menu. Because the value is undocumented, a future Windows version may ignore it; Undo removes it either way.

## When not to use it
If you send videos or pictures to another device with this entry.

## Sources
1. https://www.howtogeek.com/720449/how-to-remove-cast-to-device-option-from-windows-10-context-menu/
