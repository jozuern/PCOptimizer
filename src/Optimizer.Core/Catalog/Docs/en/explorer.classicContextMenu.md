# Classic right-click menu

## Summary
Brings back the full Windows 10 style right-click menu in File Explorer, without the "Show more options" step.

## How it works
An empty InprocServer32 key for a specific CLSID in your user profile makes Explorer skip the new compact menu [1]. Undo removes the key again. Sign out and in again (or restart Explorer) to apply.

## Why it can help
Faster access to all menu entries (for example archive tools).

## Evidence
No performance effect.

## Trade-offs & risks
Purely a usability change; a future Windows update may ignore it.

## When not to use it
Keep the new menu if you like it.

## Sources
1. https://github.com/ChrisTitusTech/winutil
