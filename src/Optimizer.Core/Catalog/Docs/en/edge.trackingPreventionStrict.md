# Microsoft Edge: strict tracking prevention

## Summary
Sets Edge tracking prevention to Strict, which blocks most trackers across sites. Some sites may not work as expected. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
The policy TrackingPrevention has four levels: 0 off, 1 basic, 2 balanced and 3 strict [1]. Balanced blocks harmful trackers and trackers from sites you have not visited; strict blocks harmful trackers and most trackers from all sites, and Microsoft notes that some parts of sites might not work [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
Fewer trackers can follow you from site to site. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1].

## Trade-offs & risks
Embedded videos, sign-in buttons or comment sections from other sites can break; you can no longer lower the level for a site in Edge settings. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If sites you need break with strict blocking; balanced is the safer choice.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/trackingprevention
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
