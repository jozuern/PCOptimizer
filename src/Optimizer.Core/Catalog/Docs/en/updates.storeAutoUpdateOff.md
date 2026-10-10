# Microsoft Store: no automatic app updates

## Summary
Apps from the Microsoft Store, including Store games, are no longer updated automatically. You update them in the Store yourself. Pro, Enterprise and Education.

## How it works
The policy for automatic app updates from the Microsoft Store (AutoDownload = 2 under SOFTWARE\Policies\Microsoft\WindowsStore) turns off automatic download and install of app updates [1][2].

## Why it can help
No Store downloads start in the background while you play or on a metered connection.

## Evidence
A documented Windows policy [1][2]. Store downloads use bandwidth and disk only while they run.

## Trade-offs & risks
Store apps stay on old versions until you update them, including fixes for security problems.

## When not to use it
If you want Store apps to stay up to date without thinking about it.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
