# Telemetry to minimum

## Summary
Sets diagnostic data to the lowest level your edition allows, disables the DiagTrack service and the compatibility and CEIP tasks. Not available on Insider builds.

## How it works
The policy AllowTelemetry = 0 requests "Diagnostic data off". Only Enterprise, Education and Server honor that level; on Pro the value counts as Required diagnostic data [1][2]. The policy also locks the diagnostic data switch in Settings [2]. The Connected User Experiences and Telemetry service (DiagTrack) is set to Disabled and stops after the next restart. The Compatibility Appraiser, CEIP, Autochk proxy and disk diagnostic tasks are disabled; tasks that do not exist on your build are skipped.

## Why it can help
These tasks and the service run at times Windows chooses, for example to inventory installed programs for upgrade compatibility [1]. Disabling them removes that background work and sends less data.

## Evidence
No published measurement shows a frame rate effect. The benefit is privacy and fewer background tasks, not speed.

## Trade-offs & risks
Windows Insider builds require optional diagnostic data, so this tweak is blocked on Insider PCs [3]. Settings shows the diagnostic data page as managed by your organization while the policy is set. Troubleshooters and Feedback Hub have less information.

## When not to use it
Do not use it on Windows Insider builds, or on PCs whose administrator relies on Windows Update reports.

## Sources
1. https://learn.microsoft.com/en-us/windows/privacy/configure-windows-diagnostic-data-in-your-organization
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system#allowtelemetry
3. https://learn.microsoft.com/en-us/windows-insider/data-settings
