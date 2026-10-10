# No cloud content, account notices and Windows tips

## Summary
Windows shows its default content instead of cloud content, no notices about your Microsoft account state and no Windows tips. Microsoft supports this only on Enterprise and Education.

## How it works
Three device policies [1]: "Turn off cloud optimized content" makes Windows experiences show the default fallback content; "Turn off cloud consumer account state content" does the same for content about your consumer account; "Do not show Windows tips" (DisableSoftLanding) turns off Windows tips [1]. Microsoft supports them only on Enterprise and Education, so the app offers the tweak only there [1].

## Why it can help
Fewer cloud-delivered promotions and reminders in Windows. A privacy setting without effect on frame rate or latency.

## Evidence
Documented Windows policies [1].

## Trade-offs & risks
You also miss tips that explain new Windows features.

## When not to use it
If you want Windows to point out new features.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience
