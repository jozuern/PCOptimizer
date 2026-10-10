# Windows apps: no unpaired or trusted devices

## Summary
Windows apps (apps from the Store and other packaged apps) can no longer talk to unpaired wireless devices or use trusted devices. Desktop programs are not affected. Pro, Enterprise and Education.

## How it works
The policies LetAppsSyncWithDevices and LetAppsAccessTrustedDevices set to Force Deny (2) stop Windows apps from communicating with unpaired wireless devices and accessing trusted devices, and the matching switch under Settings > Privacy & security is locked [1][2]. Desktop programs, such as most games, launchers and browsers, are not covered by this policy. An app that is open when the policy changes notices it only after it restarts [1].

## Why it can help
Apps you never meant to use this feature cannot use it, whatever they ask for at install time.

## Evidence
A documented Windows policy [1]; Microsoft lists it for Pro, Enterprise and Education [1] and names it in its guide to Windows connections [2]. A privacy setting without effect on frame rate or latency.

## Trade-offs & risks
Store apps that set up wireless devices, such as some smart home or fitness apps, cannot reach them. The switch in Settings stays locked until you undo the tweak.

## When not to use it
If a Windows app you use needs to talk to unpaired wireless devices or use trusted devices.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
