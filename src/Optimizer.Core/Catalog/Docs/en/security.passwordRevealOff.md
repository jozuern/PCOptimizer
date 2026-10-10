# No button to reveal passwords

## Summary
Removes the eye button that shows a typed password in Windows password fields. Pro, Enterprise and Education.

## How it works
With the policy DisablePasswordReveal = 1 the password reveal button is not displayed after a user types a password in password entry fields [1].

## Why it can help
Someone at your PC cannot show a password that is typed but not yet submitted.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
You cannot check a long password before submitting it.

## When not to use it
If you often check typed passwords with the eye button.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-credentialsui
