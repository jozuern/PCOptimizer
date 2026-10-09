# Multimedia scheduler priorities for games

## Summary
Raises the "Games" priorities of the Multimedia Class Scheduler and lowers the CPU share it reserves for background work. Often suggested, effect disputed.

## How it works
The Multimedia Class Scheduler service (MMCSS) boosts threads that register for a task such as "Games" or "Audio" [1]. SystemResponsiveness sets how much CPU time is reserved for lower-priority work (default 20 %, here 10 %). The Games task values raise GPU and scheduling priority for registered threads.

## Why it can help
Threads that register with MMCSS under "Games" get more CPU time when the system is busy.

## Evidence
Few games register their threads with MMCSS under the "Games" task, so most games are not affected. Benchmarks show no consistent difference.

## Trade-offs & risks
Background tasks get slightly less CPU time while multimedia threads are active.

## When not to use it
Not needed. It does no harm, but do not expect a measurable gain.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
