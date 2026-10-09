# Limit diagnostic logs and memory dumps

## Summary
Stops Windows from uploading extra diagnostic logs and full memory dumps with optional diagnostic data.

## How it works
Two policies limit what optional diagnostic data may contain: no additional diagnostic logs and only small crash dumps [1].

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Microsoft gets less detail to analyze crashes on your PC.

## When not to use it
If Microsoft support asks you for full diagnostics.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system
