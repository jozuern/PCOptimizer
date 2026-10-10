# Microsoft Edge: no visual search on images

## Summary
Removes visual search from image hover, the context menu and the sidebar. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
Visual search lets you explore related content about things in an image [1]. With the policy VisualSearchEnabled set to off, it is no longer offered on image hover, in the context menu or in the sidebar search [1]. Visual search in Web Capture has its own policy [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
No search button over images while you browse, and images are not sent for a visual search by a mis-click. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1].

## Trade-offs & risks
You cannot search the web for an image from Edge directly. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you use visual search on images.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/visualsearchenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
