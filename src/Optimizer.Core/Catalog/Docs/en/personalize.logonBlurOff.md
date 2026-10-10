# Sign-in screen: clear background

## Summary
Shows the sign-in screen background without the frosted blur effect.

## How it works
The policy "Show clear logon background" (DisableAcrylicBackgroundOnLogon = 1) under System > Logon turns off the acrylic blur on the sign-in screen background [1].

## Why it can help
You see your lock screen picture sharp on the sign-in screen.

## Evidence
A documented Windows policy [1]. A visual choice; no effect on frame rate or latency.

## Trade-offs & risks
None beyond the look.

## When not to use it
If you prefer the blurred background.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-logon
