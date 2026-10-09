# Widgets off

## Summary
Turns off the Widgets board and its news feed via policy. The Widgets host processes no longer run in the background.

## How it works
The policy AllowNewsAndInterests = 0 disables Widgets for the PC [1]. The taskbar button disappears.

## Why it can help
The Widgets host and its web content use memory and occasional CPU time; turning them off frees both.

## Evidence
The saving is a few hundred MB of RAM at most; no measurable frame-rate effect on PCs with enough memory.

## Trade-offs & risks
No weather or news on the taskbar.

## When not to use it
Keep Widgets if you use them.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-newsandinterests
