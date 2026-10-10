# Taskbar: no badges on apps

## Summary
Hides the small counters and status badges on taskbar app icons, like the switch "Show badges on taskbar apps".

## How it works
Taskbar behaviors include showing badges on taskbar buttons [1]. The app sets TaskbarBadges to 0. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
Fewer distractions from unread counters.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
You do not see unread counts at a glance.

## When not to use it
If you use the counters to keep track of messages.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
