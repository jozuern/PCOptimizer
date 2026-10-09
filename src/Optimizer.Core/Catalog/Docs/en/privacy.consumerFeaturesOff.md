# Consumer features off

## Summary
Turns off Microsoft consumer experiences such as suggested apps in Start. Microsoft supports this policy only on Enterprise and Education, not on Home or Pro.

## How it works
The policy DisableWindowsConsumerFeatures = 1 turns off experiences that are typically for consumers, such as Start suggestions, membership notifications, app installs after setup and redirect tiles [1]. Microsoft also lists this value in its guide for limiting connections from Windows on Enterprise [2].

## Why it can help
On Enterprise and Education it prevents promoted apps from being installed and updated in the background.

## Evidence
No direct frame rate effect. Microsoft lists the policy for Enterprise, Education and IoT Enterprise only [1].

## Trade-offs & risks
On Home and Pro the setting is not supported by Microsoft and may have no effect, so the app marks the tweak as not applicable there. Use the suggestion switches in Settings instead.

## When not to use it
Not useful on Home or Pro.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
