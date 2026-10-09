# Customer Experience Improvement Program off

## Summary
Turns off the older Customer Experience Improvement Program, which sends usage statistics.

## How it works
The policy "Turn off Windows Customer Experience Improvement Program" is turned on [1], which sets CEIPEnable to 0.

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency. Windows 10 and 11 send diagnostic data through a newer service; this older program probably sends little or nothing on Windows 11, which Microsoft does not state.

## Trade-offs & risks
None in normal use.

## When not to use it
No reason to keep it on.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-icm
