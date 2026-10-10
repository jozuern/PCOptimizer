# Microsoft Edge: no tab organization service

## Summary
Edge no longer sends URLs and titles of your tabs to its tab organization service to suggest tab groups and group names. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
When you create a tab group or use certain group similar tabs features, Edge sends URLs, page titles and existing groups to its tab organization service [1]. With the policy TabServicesEnabled set to off, no data is sent and these suggestions are not available [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
Your open tabs stay on the PC. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1].

## Trade-offs & risks
No automatic group names and no group similar tabs suggestions. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you let Edge group your tabs.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/tabservicesenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
