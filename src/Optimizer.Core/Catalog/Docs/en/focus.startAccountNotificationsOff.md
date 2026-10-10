# Start: no account notifications

## Summary
Stops the account prompts on your user picture in Start, such as backing up the device, cloud storage or subscriptions. Pro and up.

## How it works
The policy "Turn off account notifications in Start" stops Windows from showing notifications for Microsoft accounts and local users on the user tile in Start [1]. These include prompts to sign in again, back up the device, manage cloud storage, or manage a Microsoft 365 or Xbox subscription [1]. The app sets DisableAccountNotifications to 1 in the user policy key.

## Why it can help
Fewer prompts in Start that push accounts, backup or subscriptions.

## Evidence
Microsoft documents the policy for Pro, Enterprise and Education from Windows 11 24H2 [1]. It has no effect on frame rate or latency.

## Trade-offs & risks
You may miss a prompt that matters, such as a sign-in your account needs. You still get these through Settings and the apps themselves.

## When not to use it
If you rely on these reminders, for example for backup.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-notifications
