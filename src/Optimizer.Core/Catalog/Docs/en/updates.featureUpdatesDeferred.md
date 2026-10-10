# Feature updates one year later

## Summary
Windows offers a new feature update (the yearly new version) only 365 days after its release. Monthly security updates keep coming. Pro, Enterprise and Education.

## How it works
The Windows Update policy for feature updates lets a PC defer the next feature update by up to 365 days on the general availability channel [1]. The app sets DeferFeatureUpdates = 1 and DeferFeatureUpdatesPeriodInDays = 365 [1]. Quality updates, which include the security fixes, are offered as usual [1].

## Why it can help
A new Windows version reaches you after a year of fixes, and game, driver and anti-cheat makers have had time to support it.

## Evidence
A documented Windows Update policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
New Windows features arrive a year later. When your Windows version reaches the end of its support, update it yourself. Undo removes the policy.

## When not to use it
If you want new Windows features early, or run an Insider build (preview channels allow only 14 days [1]).

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
