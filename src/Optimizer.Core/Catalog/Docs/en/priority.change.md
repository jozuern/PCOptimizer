# Start priority for a program

## Summary
Windows starts this program with the chosen CPU priority, and optionally low disk and memory priority, every time, without a program running in the background. Realtime is never offered.

## How it works
Every process has a priority class; Windows gives processor time to higher classes first [1]. Image File Execution Options are per-program settings Windows reads when the program starts [2]. The app writes CpuPriorityClass (IoPriority for low disk priority, PagePriority for low memory priority) under Image File Execution Options\<program>\PerfOptions. Memory priority is a hint for the memory manager: when it takes pages back from programs, it takes them from lower priorities first [3]. Microsoft documents the priority classes, memory priority and Image File Execution Options, but does not document the PerfOptions values. Tests in a virtual machine with Windows 11 confirmed that Windows applies them: a program started with a rule for Below normal and low disk priority ran with both, and with Normal again after Undo; with PagePriority 1, 2 or 3 it started with that memory priority, and with normal (5) again without the value.

## Why it can help
A game on Above normal keeps its share of the processor when background programs get busy; a backup or download tool on Below normal or Low stays out of the way. Low memory priority for a launcher or chat program that stays open next to a game makes Windows take memory from it first when memory runs short.

## Evidence
Raising a game's priority helps only when other programs compete for the processor at the same time; on a mostly idle PC nothing changes. Low memory priority only matters when memory runs short. No published measurement shows a general frame rate gain from either, so the impact is rated as disputed.

## Trade-offs & risks
High priority can make the rest of Windows slow to respond while the program is busy [1]. Some anti-cheat systems check Image File Execution Options; if a game will not start, remove the rule. The rule applies to every program with this file name. A program on low memory priority can feel slow to come back after it sat in the background while memory was short.

## When not to use it
For programs that already run well, and with High for anything that uses the processor fully for a long time.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/gflags-overview
3. https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-memory_priority_information
