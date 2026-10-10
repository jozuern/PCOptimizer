# Windows Media DRM: no internet access

## Summary
The old Windows Media Digital Rights Management can no longer go online for licenses and security upgrades. Streaming services and games are not affected.

## How it works
The policy DisableOnline = 1 prevents Windows Media DRM from accessing the internet or intranet for license acquisition and security upgrades [1].

## Why it can help
One fewer component that connects online by itself. It matters only for old DRM-protected Windows Media files.

## Evidence
A documented Windows policy [1]. A privacy setting without effect on frame rate or latency.

## Trade-offs & risks
Protected WMA and WMV files that need a new license no longer play.

## When not to use it
If you play old protected Windows Media files.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowsmediadrm
