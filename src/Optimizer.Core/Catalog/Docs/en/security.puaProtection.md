# Microsoft Defender: block potentially unwanted apps

## Summary
Defender blocks potentially unwanted software, such as bundled adware and toolbars, when it downloads or tries to install. Pro, Enterprise and Education.

## How it works
The Defender policy PUAProtection decides whether detection of potentially unwanted applications blocks, audits or allows them; not configured is the same as disabled [1]. The app sets Block (1) [1]. It works when Microsoft Defender is the active antivirus.

## Why it can help
Installers that bring adware or browser changes along are stopped before they install.

## Evidence
A documented Defender policy [1]. Scanning costs only a little time while files download or install.

## Trade-offs & risks
Defender can block a download you wanted, for example some free tools with bundled offers; you then allow it in Windows Security > Protection history.

## When not to use it
If you use another antivirus, or often install tools that Defender rates as potentially unwanted.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-defender
