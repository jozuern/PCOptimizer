# Conservative services preset

## Summary
Sets three services that most PCs do not need to Manual: Downloaded Maps Manager, Program Compatibility Assistant and Distributed Link Tracking Client.

## How it works
The three services are set to start on demand (Manual) instead of with Windows [2]. They are not disabled: an app or a Windows trigger can still start them. The Program Compatibility Assistant has such triggers, so it still runs when needed.

## Why it can help
Fewer services start with Windows, so there is a little less background activity.

## Evidence
Idle services use almost no processor time; there is no measurable frame rate gain, which is why the impact is rated 0. Microsoft's service guidance for Windows Server 2016 rates the maps and compatibility services as safe to disable [1]; there is no such list for Windows 11.

## Trade-offs & risks
The Maps app is deprecated, so the maps service has little to do [3]. Distributed Link Tracking no longer runs, so shortcuts to files you moved to another drive or PC are not repaired automatically. The compatibility assistant keeps working through its triggers.

## When not to use it
In networks where links to files on other PCs must keep working after files move.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/security/windows-services/security-guidelines-for-disabling-system-services-in-windows-server
2. https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-changeserviceconfigw
3. https://learn.microsoft.com/en-us/windows/whats-new/deprecated-features
