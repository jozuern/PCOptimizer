# Notification center removed from the taskbar

## Summary
Removes the notification center from the notification area of the taskbar. Notifications that arrive while you are away cannot be read later.

## How it works
The policy "Remove Notifications and Action Center" (DisableNotificationCenter = 1) removes notifications and the action center from the notification area at the right end of the taskbar [1]. You can still read notifications when they appear, but not review the ones you missed [1]. It takes effect after a restart [1].

## Why it can help
One fewer panel to open by mistake, for example on a touch screen or with a misplaced click in a game.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
Missed notifications are gone once they disappear. Undo brings the notification center back after the next restart.

## When not to use it
If you read notifications later from the list.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-taskbar
