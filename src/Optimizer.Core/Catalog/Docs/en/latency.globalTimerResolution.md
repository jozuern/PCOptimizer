# Global timer resolution requests

## Summary
Expert: makes timer-resolution requests of one program apply system-wide again, as before Windows 10 2004. Effect disputed.

## How it works
Programs can ask for a finer system timer with timeBeginPeriod. Since Windows 10 version 2004 that request only affects the requesting process [1]. The value GlobalTimerResolutionRequests = 1 restores the old system-wide behavior.

## Why it can help
Tools that set a 0.5 ms timer for frame limiting or input polling affect all processes again, as some older guides assume.

## Evidence
Most games set their own timer resolution, so they do not need this. Measured differences are rare.

## Trade-offs & risks
Higher idle power, especially on laptops, when any program requests a fine timer.

## When not to use it
Only useful if you rely on an external timer tool. Not on laptops.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/timeapi/nf-timeapi-timebeginperiod
