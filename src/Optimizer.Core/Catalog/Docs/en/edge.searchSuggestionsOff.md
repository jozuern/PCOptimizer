# Microsoft Edge: no search and site suggestions while typing

## Summary
Edge no longer sends what you type in the address bar to the search engine for suggestions. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
With the policy SearchSuggestEnabled set to off, Edge shows no web search and site suggestions in the address bar while you type [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
Your typing stays on the PC until you press Enter. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1].

## Trade-offs & risks
You type addresses and searches in full. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you rely on suggestions while typing.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/searchsuggestenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
