# Microsoft Edge: no browsing data for personalization

## Summary
Stops Microsoft from collecting your Edge browsing history, favorites, collections and usage to personalize ads, search, news and other Microsoft services.

## How it works
The policy PersonalizationReportingEnabled set to off prevents Microsoft from collecting Edge browsing history, favorites and collections, usage and other browsing data for personalizing advertising, search, news, Edge and other Microsoft services [1]. The matching switch in Edge settings can then no longer be turned on [1].

## Why it can help
Less of your browsing is used for advertising and personalized content. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy that also applies to profiles signed in with a Microsoft account [1].

## Trade-offs & risks
News, search and suggestions in Microsoft services are less tailored to you. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you like personalized news and suggestions in Edge and other Microsoft services.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/personalizationreportingenabled
