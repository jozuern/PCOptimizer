# Microsoft Edge: only required diagnostic data

## Summary
Edge sends Microsoft only the required diagnostic data that keeps it secure and up to date, no optional data about browsing, visited websites and crashes.

## How it works
The policy DiagnosticData has three levels: 0 off, 1 required data, 2 optional data [1]. Optional data includes how you use the browser, the websites you visit and crash reports [1]. The app sets level 1. Microsoft does not recommend level 0, so the app does not use it [1]. Edge applies the change after it restarts [1].

## Why it can help
Less data about your browsing leaves this PC. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1]. Unlike many Edge policies it also applies to profiles signed in with a Microsoft account [1].

## Trade-offs & risks
Microsoft gets fewer crash reports from your PC to find Edge problems. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you want to help Microsoft improve Edge with optional data, or you test Edge preview versions.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/diagnosticdata
