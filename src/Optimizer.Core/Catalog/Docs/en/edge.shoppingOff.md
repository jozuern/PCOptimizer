# Microsoft Edge: no shopping features

## Summary
Turns off price comparison, coupons, rebates and express checkout on shopping sites. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
With the policy EdgeShoppingAssistantEnabled on (the default), Edge applies shopping features to retail sites automatically and fetches coupons and prices from other retailers from a server [1]. Set to off, Edge no longer looks for price comparisons, coupons, rebates or express checkout [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
Edge stops sending the shops you visit to its shopping service and shows fewer pop-ups on shopping sites. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1]; Edge applies it without a restart [1].

## Trade-offs & risks
You lose automatic coupons and price comparisons. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you use the coupons or price comparison in Edge.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/edgeshoppingassistantenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
