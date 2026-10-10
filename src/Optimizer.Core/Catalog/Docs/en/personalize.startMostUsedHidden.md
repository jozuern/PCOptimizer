# Start: no most used apps

## Summary
Hides the list of most used apps in Start and locks the matching switch in Settings. Pro, Enterprise and Education.

## How it works
The policy ShowOrHideMostUsedApps has three values: 0 user decides, 1 always show, 2 always hide the most used list; with 1 or 2 the Settings toggle is disabled [1]. The app sets 2 [1]. If the taskbar or Start does not change right away, sign out and in again.

## Why it can help
Others looking at your Start menu do not see which apps you use most.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
The Settings toggle stays locked until you undo the tweak.

## When not to use it
If you start apps from the most used list.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-start
