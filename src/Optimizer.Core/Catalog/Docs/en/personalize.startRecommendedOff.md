# Start without Recommended

## Summary
Removes the Recommended section (recent files, new apps, tips) from Start, so Start shows only pinned apps and the app list. A documented policy for Pro and up.

## How it works
Start shows recently opened files and recently added apps below the pinned apps. The policy HideRecommendedSection set to 1 removes that section [1]. Microsoft lists it for Pro, Enterprise and Education on Windows 11 22H2 and later [1]. A test in a virtual machine with Windows 11 Pro confirmed it: with the policy, Start showed no Recent section; without it, the section came back.

## Why it can help
Start is tidier, and recently opened files no longer show there for anyone who looks at your screen.

## Evidence
A documented Windows policy [1], confirmed on Windows 11 Pro in a VM. No performance effect.

## Trade-offs & risks
Recently opened files and new apps are no longer one click away in Start. Start may show a note that some settings are managed by your organization.

## When not to use it
If you open recent files from Start.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-start
