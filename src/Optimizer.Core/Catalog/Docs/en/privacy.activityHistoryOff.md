# Activity history off

## Summary
Stops Windows from recording your activity history (opened apps, files and websites) on this PC.

## How it works
Three policies under System turn off the activity feed and the publishing and upload of user activities [1]. The switch Settings > Privacy & security > Activity history > Store my activity history on this device is then off and locked.

## Why it can help
Less background logging. No performance effect is expected or documented.

## Evidence
Privacy setting; no frame rate effect.

## Trade-offs & risks
On Windows 11 activity history is stored only on the device; Microsoft removed the upload to the cloud in January 2024 [2]. Features that read the local history, such as Edge activity in Windows, lose it.

## When not to use it
Keep it if you use a feature that relies on the local activity history.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
2. https://support.microsoft.com/en-us/windows/windows-activity-history-and-your-privacy-2b279964-44ec-8c2f-e0c2-6779b07d2cbd
