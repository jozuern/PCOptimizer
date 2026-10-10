# Microsoft Edge: no network prediction

## Summary
Edge no longer resolves, connects to or preloads pages you might open next. Pages may open a little slower. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
The policy NetworkPredictionOptions controls DNS prefetching, TCP and SSL preconnection and prerendering of web pages [1]. The app sets 2, do not predict network actions on any connection [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
Edge makes no connections to sites only because a link to them is on the page. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1].

## Trade-offs & risks
Pages you open from links or search results can start loading a little later. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If page loading speed matters more to you than these extra connections.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/networkpredictionoptions
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
