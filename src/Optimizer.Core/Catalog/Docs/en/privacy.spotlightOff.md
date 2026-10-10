# Windows Spotlight off

## Summary
Turns off all Windows Spotlight features at once, including lock screen Spotlight, Windows tips and consumer features. Microsoft supports this only on Enterprise and Education.

## How it works
The user policy "Turn off all Windows spotlight features" (DisableWindowsSpotlightFeatures = 1) turns off Spotlight on the lock screen, Windows tips, Microsoft consumer features and related features at once; Microsoft recommends it for keeping network traffic low [1][2]. Microsoft supports the Spotlight policies only on Enterprise and Education, so the app offers the tweak only there [1].

## Why it can help
No pictures, tips or promotions downloaded for the lock screen and desktop. A privacy setting without effect on frame rate or latency.

## Evidence
A documented Windows policy [1][2].

## Trade-offs & risks
You choose your own lock screen picture instead of the daily Spotlight image.

## When not to use it
If you like the daily Spotlight pictures.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
