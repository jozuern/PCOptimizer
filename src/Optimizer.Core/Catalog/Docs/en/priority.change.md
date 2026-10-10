# Start priority for a program

## Summary
Windows starts this program with the chosen CPU priority, and optionally a low disk priority, every time, without a program running in the background. Realtime is never offered.

## How it works
Every process has a priority class; Windows gives processor time to higher classes first [1]. Image File Execution Options are per-program settings Windows reads when the program starts [2]. The app writes CpuPriorityClass (and IoPriority for low disk priority) under Image File Execution Options\<program>\PerfOptions. Microsoft documents the priority classes and Image File Execution Options, but does not document the PerfOptions values, so the rule is a Preview until a test confirms it.

## Why it can help
A game on Above normal keeps its share of the processor when background programs get busy; a backup or download tool on Below normal or Low stays out of the way.

## Evidence
Raising a game's priority helps only when other programs compete for the processor at the same time; on a mostly idle PC nothing changes. No published measurement shows a general frame rate gain, so the impact is rated as disputed.

## Trade-offs & risks
High priority can make the rest of Windows slow to respond while the program is busy [1]. Some anti-cheat systems check Image File Execution Options; if a game will not start, remove the rule. The rule applies to every program with this file name.

## When not to use it
For programs that already run well, and with High for anything that uses the processor fully for a long time.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/gflags-overview
