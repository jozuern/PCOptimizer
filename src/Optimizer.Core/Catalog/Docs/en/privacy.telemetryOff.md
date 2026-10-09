# Telemetry to minimum

## Summary
Sets diagnostic data to the lowest level the edition allows, disables the DiagTrack service and stops the compatibility and CEIP scheduled tasks.

## How it works
The policy AllowTelemetry = 0 requests the lowest diagnostic data level (Home and Pro treat it as "Required") [1]. The Connected User Experiences and Telemetry service (DiagTrack) is set to Disabled, and the Compatibility Appraiser, CEIP and disk diagnostic tasks are disabled [2].

## Why it can help
The Compatibility Appraiser task can use noticeable CPU and disk time when it runs; disabling it avoids that during play.

## Evidence
For frame rates the effect is usually zero; the benefit is less background activity at random times and less data sent.

## Trade-offs & risks
Windows Insider builds need optional diagnostic data, so this is blocked on Insider PCs. Some troubleshooters and feedback features have less information.

## When not to use it
Do not use on Windows Insider builds.

## Sources
1. https://learn.microsoft.com/en-us/windows/privacy/configure-windows-diagnostic-data-in-your-organization
2. https://github.com/ChrisTitusTech/winutil
