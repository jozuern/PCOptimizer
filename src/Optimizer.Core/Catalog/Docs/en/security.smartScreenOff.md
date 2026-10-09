# SmartScreen for apps off

## Summary
Expert, security trade-off: stops Windows SmartScreen from checking downloaded programs before they run. No performance effect.

## How it works
The policy EnableSmartScreen = 0 turns off SmartScreen checks for apps and files in Windows [1].

## Why it can help
Removes the "Windows protected your PC" prompt for unknown programs.

## Evidence
No frame-rate or load-time effect.

## Trade-offs & risks
Malicious downloads are no longer checked against Microsoft's reputation service.

## When not to use it
Do not use unless you understand the risk; unblocking a single file in its Properties is usually enough.

## Sources
1. https://learn.microsoft.com/en-us/windows/security/operating-system-security/virus-and-threat-protection/microsoft-defender-smartscreen/
