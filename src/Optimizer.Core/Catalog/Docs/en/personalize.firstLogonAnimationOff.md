# No first sign-in animation

## Summary
New user accounts and the first sign-in after setup skip the "Hi" animation and go to the desktop. Pro, Enterprise and Education.

## How it works
The policy EnableFirstLogonAnimation controls whether users see the first sign-in animation when they sign in to the computer for the first time [1]. The app sets it to 0 [1].

## Why it can help
Useful when you create accounts for others or test with new accounts: the first sign-in starts without the animation screens.

## Evidence
A documented Windows policy [1]. It changes only the first sign-in of an account; no effect on frame rate or latency.

## Trade-offs & risks
None beyond the missing animation.

## When not to use it
If you like the welcome animation.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowslogon
