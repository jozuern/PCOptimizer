# No minimizing by shaking a window

## Summary
Shaking a window by its title bar no longer minimizes all other windows.

## How it works
The policy "Turn off Aero Shake window minimizing mouse gesture" (NoWindowMinimizingShortcuts = 1) stops windows from being minimized or restored when the active window is shaken with the mouse [1]. It takes effect after you sign out and in again.

## Why it can help
A quick drag of a window no longer makes everything else disappear.

## Evidence
A documented Windows policy [1]. A personal preference without effect on frame rate or latency.

## Trade-offs & risks
None beyond losing the gesture.

## When not to use it
If you use the shake gesture.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-desktop
