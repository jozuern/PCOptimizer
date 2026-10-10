# Taskbar: no show desktop corner

## Summary
Clicking the far right corner of the taskbar no longer minimizes all windows.

## How it works
Taskbar behaviors include "Select the far corner of the taskbar to show the desktop" [1]. The app sets TaskbarSd to 0. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
A misplaced click in the corner no longer minimizes your game or work.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
Use Windows + D to show the desktop instead.

## When not to use it
If you use the corner to reach the desktop.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
