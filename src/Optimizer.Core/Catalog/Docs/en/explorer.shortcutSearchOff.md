# Broken shortcuts: no drive search

## Summary
When a shortcut's target is gone, Windows says so right away instead of searching drives for the file.

## How it works
Without these policies Windows looks for the missing target of a shortcut: it searches all paths linked to the shortcut and uses NTFS file tracking [1]. NoResolveSearch = 1 skips the full drive search and NoResolveTrack = 1 skips the file ID tracking [1]. Windows then shows a message that the file is not found [1].

## Why it can help
No waiting and no disk activity on slow or network drives when you click an old shortcut.

## Evidence
Documented Windows policies [1]. No effect on frame rate or latency.

## Trade-offs & risks
A shortcut to a moved file is not fixed automatically; you create a new one.

## When not to use it
If you move files often and rely on shortcuts finding them.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-startmenu
