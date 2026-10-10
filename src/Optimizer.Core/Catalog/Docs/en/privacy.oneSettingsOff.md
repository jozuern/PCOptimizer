# No configuration downloads from OneSettings

## Summary
Windows stops downloading configuration settings from the OneSettings service. Microsoft warns that apps using this service may stop working. Pro, Enterprise and Education.

## How it works
Without the policy, Windows periodically connects to the OneSettings service to download configuration settings [1]. With DisableOneSettingsDownloads = 1 it does not [1]. Windows components and apps, such as the telemetry service, use this service to update their configuration [2].

## Why it can help
One fewer regular connection from Windows to Microsoft.

## Evidence
A documented Windows policy [1][2]. No measured effect on performance.

## Trade-offs & risks
Microsoft warns that apps using this service may stop working [2], and fixes Microsoft ships as configuration changes may not reach this PC.

## When not to use it
On a PC that should keep working with as few surprises as possible; this is for people who want to limit connections as far as Microsoft allows.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
