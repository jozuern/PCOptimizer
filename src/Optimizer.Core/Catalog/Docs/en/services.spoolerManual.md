# Print Spooler to Manual

## Summary
Sets the Print Spooler to start on demand. Offered only when no printer is installed.

## How it works
The Print Spooler service is set to Manual [1]. Windows starts it when you print or add a printer.

## Why it can help
One service less running all the time; the Print Spooler has also had several security problems.

## Evidence
No measurable frame rate effect.

## Trade-offs & risks
The first print job or "Print to PDF" after starting takes a moment longer.

## When not to use it
If you print often.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/security/windows-services/security-guidelines-for-disabling-system-services-in-windows-server
