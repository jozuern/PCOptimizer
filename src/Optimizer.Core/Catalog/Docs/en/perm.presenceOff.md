# Windows apps: no presence sensing

## Summary
Windows apps (Store and other packaged apps) can no longer use presence sensing, the sensor some laptops and monitors use to notice when you sit in front of them. Pro and up.

## How it works
Some PCs have a presence sensor that notices whether someone is in front of the screen, and apps can ask for access to it. The policy LetAppsAccessHumanPresence decides whether Windows apps can access presence sensing [1]. The app sets it to Force Deny (2), so Windows apps cannot use the sensor and the matching switch under Settings > Privacy & security is locked [1]. An app that is open when the policy changes notices it only after it restarts [1]. On a PC without such a sensor the setting changes nothing you would notice.

## Why it can help
Apps you never meant to watch for your presence cannot use the sensor, whatever they ask for at install time.

## Evidence
A documented Windows policy [1]; Microsoft lists it for Windows 11 Pro, Enterprise and Education [1]. A privacy setting without effect on frame rate or latency.

## Trade-offs & risks
Windows apps that use presence sensing, for example to pause when you leave, stop doing so. The switch in Settings stays locked until you undo the change.

## When not to use it
If a Windows app you use needs presence sensing.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
