# Explorer context menu extension

## Summary
Turns this program's entries in the Explorer right-click menu on or off. Slow extensions are a common cause of a lagging context menu.

## How it works
Explorer loads the listed extension into every right-click menu. The app adds the extension to Explorer's documented block list (Shell Extensions\Blocked) instead of uninstalling it [1]. Explorer reads the list when it starts, so sign out or restart Explorer. Undo removes it from the list again.

## Why it can help
A faster right-click menu in Explorer.

## Evidence
No effect on games.

## Trade-offs & risks
The program's right-click entries disappear (for example "Open with" or archive commands).

## When not to use it
For extensions you use, such as 7-Zip or cloud storage menus.

## Sources
1. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
