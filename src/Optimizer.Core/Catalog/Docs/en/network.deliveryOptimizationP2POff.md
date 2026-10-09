# Delivery Optimization peer-to-peer off

## Summary
Stops Windows from uploading update data to other PCs and downloading from them. Updates then come only from Microsoft's servers.

## How it works
Delivery Optimization shares Windows and Store update data between PCs. The policy DODownloadMode = 0 limits it to plain HTTP downloads from Microsoft [1]. It is a policy value, so Windows Settings shows "Some settings are managed by your organization" for this page.

## Why it can help
No background uploads to other PCs, which can use upstream bandwidth while you play online.

## Evidence
Upload bandwidth is already limited by default; the effect on ping is only noticeable on slow connections.

## Trade-offs & risks
Updates on several PCs in your home network are downloaded separately.

## When not to use it
Not needed on fast connections or if you like peer-to-peer within your local network.

## Sources
1. https://learn.microsoft.com/en-us/windows/deployment/do/waas-delivery-optimization-reference
2. https://github.com/ChrisTitusTech/winutil
