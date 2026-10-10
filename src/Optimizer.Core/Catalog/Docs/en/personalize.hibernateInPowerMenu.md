# Hibernate in the power menu

## Summary
Adds Hibernate to the power menu in Start, if the PC supports it.

## How it works
The policy ShowHibernateOption = 1 shows hibernate in the power options menu, as long as the hardware supports it [1]. Without the policy you choose this in the Power Options control panel [1].

## Why it can help
Hibernate saves your open apps to disk and turns the PC off completely, which uses no power at all, unlike sleep.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
The choice in Control Panel is locked. The entry appears only where the hardware supports hibernation [1].

## When not to use it
If you never hibernate.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowsexplorer
