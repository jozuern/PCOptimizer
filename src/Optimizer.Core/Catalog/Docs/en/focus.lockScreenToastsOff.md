# No app notifications on the lock screen

## Summary
Apps can no longer show notifications on the lock screen, so message previews are not visible while the PC is locked.

## How it works
The policy "Turn off toast notifications on the lock screen" (NoToastApplicationNotificationOnLockScreen = 1) stops applications from raising toast notifications on the lock screen [1].

## Why it can help
Nobody at your locked PC can read message previews.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
You see new messages only after signing in.

## When not to use it
If you want to see notifications without unlocking.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-wpn
