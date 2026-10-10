# Google Chrome: no background mode

## Summary
Chrome no longer starts at sign-in and stops when you close its last window. Frees memory when you do not use Chrome.

## How it works
In background mode a Chrome process starts at Windows sign-in and keeps running after the last window closes, so background apps and the session stay active [1]. With the policy BackgroundModeEnabled set to off, background mode is off and users cannot turn it on [1]. Chrome reads the policy from HKLM\SOFTWARE\Policies\Google\Chrome, also on PCs without a domain [2].

## Why it can help
A Chrome process without a window takes memory and some processor time, also during games and on battery.

## Evidence
A documented Chrome policy [1]. Without the policy, background mode is off at first but can be turned on in Chrome settings [1]; how much memory the process takes depends on apps and extensions.

## Trade-offs & risks
Chrome apps and extensions no longer run with Chrome closed. Because these are policies, Chrome shows that it is managed by your organization, and the matching settings are locked. Undo removes them.

## When not to use it
If you rely on Chrome apps or notifications with Chrome closed.

## Sources
1. https://chromeenterprise.google/policies/#BackgroundModeEnabled
2. https://support.google.com/chrome/a/answer/9131254?hl=en
