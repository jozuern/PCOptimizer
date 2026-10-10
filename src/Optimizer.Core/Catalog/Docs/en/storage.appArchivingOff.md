# No automatic archiving of unused apps

## Summary
Windows no longer archives Store apps you rarely use; they stay fully installed. Pro, Enterprise and Education.

## How it works
By default Windows periodically checks for infrequently used apps and archives them, and you can change that yourself [1]. AllowAutomaticAppArchiving = 0 is an explicit deny: Windows does not archive any apps [1].

## Why it can help
Rarely used Store apps and games stay ready to start, also without an internet connection.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
Unused apps keep taking disk space.

## When not to use it
On a PC with little free space where archiving helps.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement
