# Delivery Optimization peer-to-peer off

## Summary
Stops Windows from sharing update data with other PCs. Updates then come only from Microsoft's servers. By default Windows shares only within your local network.

## How it works
Delivery Optimization can download Windows and Store updates from other PCs and upload them to other PCs. The default mode shares only with PCs on your local network [1]. The policy DODownloadMode = 0 turns peer-to-peer off and keeps plain HTTP downloads from Microsoft [1][2]. It is a policy value, so Windows Settings shows that some settings are managed by your organization on this page.

## Why it can help
No uploads to other PCs in the background. This matters for online games mainly if you had allowed uploads to PCs on the internet, or if other PCs in your home download from yours over a slow Wi-Fi.

## Evidence
In the default mode, uploads stay inside your local network and do not use your internet upload [1]. Uploads to PCs on the internet happen only if you turned that option on, and are capped at 20 GB per month by default [1].

## Trade-offs & risks
Updates on several PCs in your home network are downloaded separately.

## When not to use it
Not needed on fast connections or if you like peer-to-peer within your local network.

## Sources
1. https://learn.microsoft.com/en-us/windows/deployment/do/waas-delivery-optimization-reference
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-deliveryoptimization
