# Start: no recently added apps

## Summary
Start no longer shows the list of recently installed apps. Pro, Enterprise and Education.

## How it works
The policy HideRecentlyAddedApps = 1 prevents the Start menu from displaying a list of recently installed applications [1]. If the taskbar or Start does not change right away, sign out and in again.

## Why it can help
A tidier Start menu, also when installers add helper apps you never start.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
You find new apps in the full list or by typing their name.

## When not to use it
If you start new apps from the recently added list.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-start
