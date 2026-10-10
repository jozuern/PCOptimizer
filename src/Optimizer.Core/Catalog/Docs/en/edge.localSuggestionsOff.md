# Microsoft Edge: no suggestions from history and favorites

## Summary
The address bar no longer suggests pages from your history and favorites. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
The policy LocalProvidersEnabled set to off turns off suggestions from local providers such as your history and favorites in the address bar [1]. Edge applies it after a restart [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
People who share your PC no longer see your visited pages while typing an address.

## Evidence
A documented Edge policy [1]. The suggestions come from data on this PC, so this changes what others see on the screen, not what leaves the PC. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Trade-offs & risks
You lose quick access to pages you visited before. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you open sites by typing a few letters of their name.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/localprovidersenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
