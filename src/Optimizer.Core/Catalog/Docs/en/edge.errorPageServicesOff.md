# Microsoft Edge: no web services for error pages

## Summary
For pages that fail to load, Edge no longer asks Microsoft web services for similar pages or a connection check. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
AlternateErrorPagesEnabled set to off stops Edge from suggesting similar pages when a web page cannot be found [1]. ResolveNavigationErrorsUseWebService set to off makes Edge use native Windows APIs instead of a dataless connection to a web service to check connectivity, for example on hotel and airport Wi-Fi [2]. Since Edge 116 Microsoft lists these policies as not applied to a profile that is signed in with a personal Microsoft account [3]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
Fewer requests to Microsoft about addresses that failed. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
Documented Edge policies [1][2].

## Trade-offs & risks
Edge may detect Wi-Fi sign-in pages in hotels or airports less reliably, and offers no alternative for mistyped addresses. Because these are policies, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes them.

## When not to use it
If you often use public Wi-Fi with a sign-in page.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/alternateerrorpagesenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/resolvenavigationerrorsusewebservice
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
