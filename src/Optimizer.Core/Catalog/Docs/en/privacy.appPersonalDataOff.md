# Store app access to personal data off

## Summary
Denies Store apps access to account info, contacts, calendar, call history, email, messages, tasks and other apps' diagnostics.

## How it works
The App privacy policies "Let Windows apps access ..." are set to "Force Deny" for these data types [1]. Desktop programs (Discord, Steam, browsers) are not affected; camera and microphone stay as they are.

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Store apps such as Mail, Calendar or People cannot read this data, and the switches in Settings are locked.

## When not to use it
If you use the Store Mail, Calendar or People apps.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
