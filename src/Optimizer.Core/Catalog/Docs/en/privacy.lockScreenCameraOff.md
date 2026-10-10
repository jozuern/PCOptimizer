# No camera on the lock screen

## Summary
Nobody can open the camera from the lock screen, and the switch for it in Settings is locked. Pro, Enterprise and Education.

## How it works
The policy "Prevent enabling lock screen camera" (NoLockScreenCamera = 1) disables the lock screen camera switch in Settings and prevents a camera from being started on the lock screen [1].

## Why it can help
Someone at your locked PC cannot use the camera.

## Evidence
A documented Windows policy [1]. A privacy setting without effect on frame rate or latency.

## Trade-offs & risks
You cannot take a quick picture from the lock screen; most desktop PCs never offer it.

## When not to use it
If you use the camera from the lock screen on a tablet.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-devicelock
