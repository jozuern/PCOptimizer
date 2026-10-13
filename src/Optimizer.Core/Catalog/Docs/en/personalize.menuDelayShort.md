# Faster submenus

## Summary
Submenus open after 100 ms instead of Windows' 400 ms when the mouse rests on a menu entry with an arrow. Takes effect at once, no sign-out.

## How it works
When the pointer rests on a menu entry that has a submenu, Windows waits before it opens the submenu. The app sets this wait with the documented SystemParametersInfo function (SPI_SETMENUSHOWDELAY) to 100 ms and lets Windows save it to your profile, so the change applies at once and stays [1]. Undo sets the value you had before.

## Why it can help
Menus with submenus, such as the classic context menu and menus in older programs, open faster and feel more direct.

## Evidence
No performance effect: only the wait before a submenu opens changes. The setting itself is documented [1].

## Trade-offs & risks
Submenus also open when the pointer only passes over an entry, which some people find jumpy.

## When not to use it
If submenus already open too eagerly for you.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
