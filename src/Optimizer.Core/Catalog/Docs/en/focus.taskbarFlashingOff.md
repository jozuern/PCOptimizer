# Taskbar: no flashing buttons

## Summary
Taskbar buttons no longer flash when an app wants your attention, like the switch "Show flashing on taskbar apps".

## How it works
Apps flash their taskbar button when they need your interaction, for example when they open behind another window [1]. The app sets TaskbarFlashing to 0. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
No flashing taskbar while you play in borderless windowed mode or stream.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
You notice later that an app waits for you.

## When not to use it
If you rely on flashing buttons to notice chat messages.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
