# Scheduled task at sign-in or boot

## Summary
Turns this scheduled task on or off. Many updaters and launchers use such tasks instead of a startup entry.

## How it works
The task is enabled or disabled in Task Scheduler; its triggers and actions are not changed [1]. Undo restores the previous state.

## Why it can help
Programs that start with Windows make signing in slower and use memory and sometimes processor time in the background.

## Evidence
Signing in gets faster; the frame rate effect depends on what the program does in the background.

## Trade-offs & risks
Updaters that use the task no longer check for updates on their own; update the program manually now and then.

## When not to use it
For tasks of security software and drivers.

## Sources
1. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
