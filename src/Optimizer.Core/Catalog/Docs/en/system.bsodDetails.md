# Blue screen shows technical details

## Summary
After a crash the blue screen also shows the parameters of the stop code, which helps when you look up the cause. Needs a restart. Value Microsoft does not document, tested.

## How it works
DisplayParameters = 1 under SYSTEM\CurrentControlSet\Control\CrashControl makes the blue screen show the parameters of the stop code [1]. The rest of the stop screen stays as it is. Microsoft does not document the value.

## Why it can help
The stop code alone often does not name the driver at fault; the parameters help to narrow it down without opening a memory dump.

## Evidence
In a VM test on Windows 11 build 26300, a stop error forced through Hyper-V showed the four parameters in small print at the top left of the stop screen with the value, and none after undo. No effect on frame rate or latency, and crash dumps are written as before.

## Trade-offs & risks
None known.

## When not to use it
If you never look at blue screens.

## Sources
1. https://winaero.com/show-bsod-details-instead-of-the-sad-smiley-in-windows-10/
