# "End task" in the taskbar menu

## Summary
Adds "End task" to the right-click menu of taskbar buttons, to close a frozen game or app without Task Manager.

## How it works
TaskbarEndTask = 1 turns on the option, the same as Settings > System > Advanced > End task (called For developers before version 25H2) [1]. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
A hung fullscreen game can be closed faster.

## Evidence
No performance effect.

## Trade-offs & risks
Ending a task discards unsaved work in that app.

## When not to use it
No reason not to use it.

## Sources
1. https://learn.microsoft.com/en-us/windows/advanced-settings/
