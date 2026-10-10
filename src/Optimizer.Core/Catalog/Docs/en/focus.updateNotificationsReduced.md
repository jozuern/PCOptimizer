# Fewer Windows Update notifications

## Summary
Turns off Windows Update notifications except restart warnings. Updates still download and install as before. Pro, Enterprise and Education.

## How it works
The policy for Windows Update notifications has three levels: 0 default, 1 turn off all notifications except restart warnings, 2 turn off all including restart warnings [1]. The app sets 1 [1]. The policy does not change how or when updates download and install [1].

## Why it can help
Fewer pop-ups about updates while you work or play, while you still learn about a pending restart.

## Evidence
A documented Windows policy [1]. Microsoft warns that without notifications and without automatic updates, nobody notices missing security updates [1]; automatic updates stay on here.

## Trade-offs & risks
You learn about new updates only in Settings > Windows Update or from the restart warning.

## When not to use it
If you have turned off automatic updates; then you need the notifications.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
