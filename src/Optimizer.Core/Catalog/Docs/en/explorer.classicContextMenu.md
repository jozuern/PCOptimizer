# Classic right-click menu

## Summary
Brings back the full Windows 10 style right-click menu in File Explorer, without the "Show more options" step.

## How it works
An empty InprocServer32 entry for the CLSID of the new File Explorer menu in your user profile overrides the system registration, so Explorer falls back to the classic menu. Microsoft does not document this; it is known from community tools [1]. Undo removes the entries the app created and leaves keys that existed before. Sign out and in again (or restart Explorer) to apply.

## Why it can help
Faster access to all menu entries (for example archive tools).

## Evidence
No performance effect.

## Trade-offs & risks
Purely a usability change. Because it is undocumented, a future Windows update may ignore it.

## When not to use it
Keep the new menu if you like it.

## Sources
1. https://github.com/ChrisTitusTech/winutil
