# Microsoft Edge: new tab page without news and promotions

## Summary
The new tab page shows no Microsoft content feed, no preset top sites and no Bing chat entry points. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
NewTabPageContentEnabled set to off removes the Microsoft content on the new tab page [1]; NewTabPageHideDefaultTopSites hides the default top site tiles [2]; NewTabPageBingChatEnabled set to off removes the three Bing chat entry points in and below the search box [3]. Since Edge 116 Microsoft lists these policies as not applied to a profile that is signed in with a personal Microsoft account [4]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
A calmer new tab page that loads less content from Microsoft's servers. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
Documented Edge policies [1][2][3].

## Trade-offs & risks
No news or weather on the new tab page. Because these are policies, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes them.

## When not to use it
If you read the news feed on the Edge new tab page.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/newtabpagecontentenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/newtabpagehidedefaulttopsites
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/newtabpagebingchatenabled
4. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
