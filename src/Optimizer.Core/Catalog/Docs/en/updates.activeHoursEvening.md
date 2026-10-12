# No update restarts from 8 AM to 2 AM

## Summary
Windows does not restart by itself for updates between 8 AM and 2 AM, so an evening game or a late download is not cut off. Pro, Enterprise and Education.

## How it works
Windows restarts after an update outside of its active hours, by default 8 AM to 5 PM [1]. The policy "Turn off auto-restart for updates during active hours" fixes the active hours: the app sets SetActiveHours = 1, ActiveHoursStart = 8 and ActiveHoursEnd = 2 [1]. 18 hours is the longest range Windows allows [1][2]. Updates still download and install as usual; only the automatic restart waits until 2 AM.

## Why it can help
With the default hours, an automatic restart can happen in the evening, when most people play. With this policy it moves to the night.

## Evidence
A documented Windows Update policy [1][2]. No effect on frame rate or latency.

## Trade-offs & risks
While the policy is set, the active hours you chose in Settings no longer apply [2]. Updates still need a restart to take effect, so the PC has to be on, or restarted by you, outside these hours [2]. Undo removes the policy.

## When not to use it
If you mostly use the PC at night, or want Windows to pick the active hours from how you use the PC.

## Sources
1. https://learn.microsoft.com/en-us/windows/deployment/update/waas-restart
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
