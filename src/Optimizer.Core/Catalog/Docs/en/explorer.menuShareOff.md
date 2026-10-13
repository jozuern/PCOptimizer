# No "Share" in the right-click menu

## Summary
Removes "Share" from the classic right-click menu of files and the Share button from the Windows 11 menu. Microsoft does not document the value; a VM test confirmed it.

## How it works
"Share" opens the Windows share window, which sends a file to an app that supports sharing [1]. It comes from a shell extension that adds the entry to the classic right-click menu (Show more options) and the Share button to the Windows 11 menu. Explorer skips shell extensions whose CLSID is listed as a value under HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked. The app adds {e2bf9676-5f8f-435c-97eb-11607a5bedf7}, the extension behind "Share", to that list [1]. Microsoft does not document this list. A test in a virtual machine with Windows 11 confirmed it: "Share" was gone from the classic menu of a video file (and the Share button from its Windows 11 menu) and came back after the value was removed. In the VM test, menus opened after the change already left the entry out, without an Explorer restart. If one still shows it, sign out and in again [1].

## Why it can help
If you never send files through the share window, the entry and the button are clutter in every file menu.

## Evidence
No performance effect; only the menu changes.

## Trade-offs & risks
The share window can no longer be opened from a file's menu. Because the value is undocumented, a future Windows version may ignore it; Undo removes it either way.

## When not to use it
If you send files to other apps through Share.

## Sources
1. https://winaero.com/remove-share-context-menu-windows-10/
