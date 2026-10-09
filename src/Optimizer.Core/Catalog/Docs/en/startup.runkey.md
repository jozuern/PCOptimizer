# Startup program (Run key)

## Summary
Turns this program on or off at sign-in, the same way Task Manager does. The program stays installed.

## How it works
Windows starts programs listed in the Run keys of the registry at sign-in. The app does not delete the entry; it marks it as disabled in StartupApproved, exactly like the Startup apps page in Task Manager and Settings [1]. Undo restores the previous state.

## Why it can help
Programs that start with Windows make signing in slower and use memory and sometimes processor time in the background.

## Evidence
Signing in gets faster; the frame rate effect depends on what the program does in the background.

## Trade-offs & risks
The program no longer starts by itself; open it from the Start menu when you need it.

## When not to use it
For drivers' helper programs you need (audio, touchpad, controller software).

## Sources
1. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
