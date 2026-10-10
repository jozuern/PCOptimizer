# Store app access to personal data off

## Summary
Denies Store apps access to account info, contacts, calendar, call history, email, messages, tasks and other apps' diagnostics.

## How it works
The App privacy policies for these data types, such as "Let Windows apps access contacts", are set to "Force Deny" [1][2]. Desktop programs (Discord, Steam, browsers) are not affected; camera and microphone stay as they are.

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Store apps that use these data types cannot read them, and the switches in Settings are locked. Apps that are open need a restart to notice the change [1]. The Windows Mail, Calendar and People apps are no longer supported [3].

## When not to use it
If a Store app you use needs your contacts, calendar, email or messages.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
3. https://support.microsoft.com/en-us/office/windows-mail-calendar-and-people-become-outlook-773ecb94-5b16-4155-96e1-bc9afdc08e31
