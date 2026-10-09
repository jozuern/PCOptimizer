# Conservative services preset

## Summary
Sets three services that most PCs do not need to Manual: Downloaded Maps Manager, Program Compatibility Assistant and Distributed Link Tracking Client.

## How it works
The services are set to start on demand (Manual) instead of with Windows [1]. They are not disabled, so anything that needs them can still start them.

## Why it can help
Slightly faster startup and a little less memory and background activity.

## Evidence
Idle services use almost no processor time; there is no measurable frame rate gain. That is why the impact is rated 0.

## Trade-offs & risks
Offline maps update only when you open Maps; the compatibility assistant no longer suggests fixes for old programs on its own.

## When not to use it
If you rely on compatibility help for old programs.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/security/windows-services/security-guidelines-for-disabling-system-services-in-windows-server
