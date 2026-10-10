# Taskbar: Task View button hidden

## Summary
Hides the Task View button on the taskbar and locks its switch in Settings. Win+Tab still opens Task View. Pro, Enterprise and Education.

## How it works
The policy "Hide the TaskView button" (HideTaskViewButton = 1) hides the button and disables the Settings toggle [1]. It needs Windows 11 22H2 or later [1]. If the taskbar or Start does not change right away, sign out and in again.

## Why it can help
More room on the taskbar for your apps.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
The Settings toggle stays locked until you undo the tweak.

## When not to use it
If you open Task View or desktops with the button.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-start
