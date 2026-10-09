# Startup program (Startup folder)

## Summary
Turns this shortcut in the Startup folder on or off, the same way Task Manager does. The file stays where it is.

## How it works
Windows starts everything in the Startup folders at sign-in. The app marks the shortcut as disabled in StartupApproved instead of deleting it [1]. Undo restores the previous state.

## Why it can help
Programs that start with Windows make signing in slower and use memory and sometimes processor time in the background.

## Evidence
Signing in gets faster; the frame rate effect depends on what the program does in the background.

## Trade-offs & risks
The program no longer starts by itself.

## When not to use it
For programs you need right after signing in.

## Sources
1. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
