# Monthly updates one week later

## Summary
Windows offers the monthly quality updates, security fixes included, 7 days after their release, so problems of the first days are known before your PC installs them. Pro, Enterprise and Education.

## How it works
The policy "Select when Quality Updates are received" lets a PC defer quality updates by up to 30 days [1]. The app sets DeferQualityUpdates = 1 and DeferQualityUpdatesPeriodInDays = 7 [1]. Feature updates have their own policy and are not affected.

## Why it can help
A monthly update that breaks a driver, a game or an anti-cheat reaches you a week later, after others have run into the problem.

## Evidence
A documented Windows Update policy [1]. No effect on frame rate or latency. Whether a given month's update has a problem is situational.

## Trade-offs & risks
Security fixes also arrive a week later, so a known vulnerability stays open for those 7 days. Undo removes the policy.

## When not to use it
If the PC handles sensitive data, is used by others, or you want security fixes on the day they are released.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
