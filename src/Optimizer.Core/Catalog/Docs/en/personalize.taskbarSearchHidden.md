# Taskbar: search hidden

## Summary
Hides search on the taskbar. The Settings switch is locked. Pro, Enterprise and Education.

## How it works
The policy "Configures search on the taskbar" has four values: 0 hide, 1 search icon only, 2 search icon and label, 3 search box [1]. The app sets 0; users can no longer change it in Settings [1]. It needs Windows 11 24H2 or later [1]. If the taskbar or Start does not change right away, sign out and in again.

## Why it can help
More room on the taskbar for your apps. Search stays one key away: press the Windows key and type.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
The taskbar choice in Settings is locked until you undo the tweak.

## When not to use it
If you like the search box on the taskbar.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search
