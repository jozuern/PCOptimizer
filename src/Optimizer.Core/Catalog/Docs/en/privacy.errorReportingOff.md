# Windows Error Reporting off

## Summary
Stops Windows from sending crash and error reports to Microsoft.

## How it works
The policy "Disable Windows Error Reporting" is set [1].

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Crash reports no longer reach Microsoft or app makers, and Windows no longer offers solutions for known problems.

## When not to use it
If you help diagnose crashes, or a developer asks for reports.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-errorreporting
