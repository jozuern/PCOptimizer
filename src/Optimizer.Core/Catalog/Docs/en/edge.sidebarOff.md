# Microsoft Edge: no sidebar

## Summary
Hides the sidebar, the launcher bar on the right side of Edge, for good. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
The policy HubsSidebarEnabled set to off means the sidebar is never shown [1]. Since Edge 141 the Copilot button in the toolbar has its own policy for work accounts, so this one does not control it [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
More room for web pages and one fewer panel that loads web content in the background. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1]; Edge applies it without a restart [1].

## Trade-offs & risks
Apps and tools you pinned to the sidebar are no longer reachable there. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you use apps or tools in the Edge sidebar.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/hubssidebarenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
