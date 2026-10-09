# Limit diagnostic logs and memory dumps

## Summary
Stops extra diagnostic logs and full memory dumps from being collected. Only has an effect if you send optional diagnostic data.

## How it works
The policies "Limit Diagnostic Log Collection" and "Limit Dump Collection" are turned on [1]. Windows Error Reporting then sends only kernel mini dumps and user mode triage dumps. Both policies matter only when the PC sends optional diagnostic data.

## Why it can help
If you send optional diagnostic data, less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Microsoft gets less detail to analyze crashes on your PC.

## When not to use it
If Microsoft support asks you for full diagnostics. With required diagnostic data only, this changes nothing.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system
