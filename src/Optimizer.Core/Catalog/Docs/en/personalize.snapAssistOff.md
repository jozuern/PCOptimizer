# Snap: no suggestions for the other half

## Summary
After you snap a window to one side, Windows no longer offers other windows to fill the rest. Snapping itself still works.

## How it works
"When I snap a window, show what I can snap next to it" turns Snap Assist on or off [1]. The app sets SnapAssist to 0. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
No thumbnails to dismiss after snapping a window.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
You place the second window yourself.

## When not to use it
If you use Snap Assist to split the screen.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/snap-your-windows
