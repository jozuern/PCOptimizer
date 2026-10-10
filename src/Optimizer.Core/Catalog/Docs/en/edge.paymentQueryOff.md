# Microsoft Edge: sites cannot check for saved payment methods

## Summary
Websites can no longer ask Edge whether you have payment methods saved. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
With the policy PaymentMethodQueryEnabled set to off, sites that use PaymentRequest.canMakePayment or hasEnrolledInstrument are told that no payment methods are available [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
One less detail sites can learn about you. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1].

## Trade-offs & risks
Some checkout pages may not offer quick payment with a saved card. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you pay with cards saved in Edge.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/paymentmethodqueryenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
