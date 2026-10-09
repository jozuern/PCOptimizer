# Widgets off

## Summary
Turns off the Widgets board and its news feed for the PC via policy, including the taskbar entry.

## How it works
The policy AllowNewsAndInterests = 0 (Allow widgets) disables the whole widgets experience, including content on the taskbar [1].

## Why it can help
The Widgets board loads web content; with the policy set, that content is no longer loaded.

## Evidence
No published measurement shows a frame rate or memory effect. The benefit is mainly fewer distractions.

## Trade-offs & risks
No weather or news on the taskbar.

## When not to use it
Keep Widgets if you use them.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-newsandinterests
