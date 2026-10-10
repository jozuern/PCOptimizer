# Windows apps: no access to your notifications

## Summary
Windows apps (apps from the Store and other packaged apps) can no longer read your notifications. Desktop programs are not affected. Pro, Enterprise and Education.

## How it works
The policy LetAppsAccessNotifications set to Force Deny (2) stops Windows apps from accessing notifications, and the matching switch under Settings > Privacy & security is locked [1][2]. Desktop programs, such as most games, launchers and browsers, are not covered by this policy. An app that is open when the policy changes notices it only after it restarts [1].

## Why it can help
Apps you never meant to use this feature cannot use it, whatever they ask for at install time.

## Evidence
A documented Windows policy [1]; Microsoft lists it for Pro, Enterprise and Education [1] and names it in its guide to Windows connections [2]. A privacy setting without effect on frame rate or latency.

## Trade-offs & risks
Apps that mirror or collect notifications, such as companion apps for watches, no longer see them. The switch in Settings stays locked until you undo the tweak.

## When not to use it
If a Windows app you use needs to read your notifications.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
