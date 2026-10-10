# Windows Ink Workspace off

## Summary
Turns off Windows Ink Workspace, the pen panel for notes and screen sketches. Pro, Enterprise and Education.

## How it works
The policy AllowWindowsInkWorkspace has three values: 0 access to Ink Workspace disabled, 1 on but not above the lock screen, 2 on everywhere (default) [1]. The app sets 0 [1].

## Why it can help
On PCs without a pen the feature is not needed; on pen PCs, the pen button no longer opens it by accident.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
Pen shortcuts that open Ink Workspace stop working.

## When not to use it
If you use a pen with Ink Workspace.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsinkworkspace
