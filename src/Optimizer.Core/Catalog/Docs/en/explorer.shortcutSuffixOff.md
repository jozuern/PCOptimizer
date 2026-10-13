# No "- Shortcut" in new shortcut names

## Summary
New shortcuts are named "Game.lnk" instead of "Game - Shortcut.lnk". Microsoft does not document the value; a VM test confirmed it.

## How it works
When you create a shortcut, Windows names it from a template, by default "%s - Shortcut.lnk", where %s is the name of the target [1]. The value ShortcutNameTemplate under HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates replaces the template; the app sets it to "%s.lnk" [1]. Microsoft does not document this value. A test in a virtual machine with Windows 11 confirmed it: the shell's Create shortcut made "Doc.lnk" with the value and "Doc - Shortcut.lnk" without it. It applies to shortcuts created from then on, without a sign-out [1].

## Why it can help
Shortcuts you put on the desktop or in a folder get a clean name without renaming each one.

## Evidence
No performance effect; only the name of new shortcuts changes. Existing shortcuts keep their names.

## Trade-offs & risks
A new shortcut is no longer marked as one by its name (the arrow on its icon still shows it). Some users reported that an update ignored the value [1]; Undo removes it either way.

## When not to use it
If you like seeing "Shortcut" in the name.

## Sources
1. https://winaero.com/disable-the-shortcut-text-addition-for-shortcuts-in-windows-11
