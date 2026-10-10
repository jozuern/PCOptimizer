# Microsoft Edge: no quick links on the new tab page

## Summary
Hides the quick links tiles on the new tab page and locks their switch. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
The policy NewTabPageQuickLinksEnabled set to off hides quick links on the new tab page and disables the quick links control in its settings [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
A tidier new tab page, for example on a shared PC. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1].

## Trade-offs & risks
Your own quick links are hidden too. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you open sites from the quick links tiles.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/newtabpagequicklinksenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
