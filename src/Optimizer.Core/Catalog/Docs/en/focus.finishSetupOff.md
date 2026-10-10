# No "Let's finish setting up your device" screen

## Summary
Turns off "Suggest ways to get the most out of Windows and finish setting up this device", the full screen setup prompt that can appear at sign-in.

## How it works
The screen "Let's finish setting up your device" may show when you sign in, to suggest ways to get the most out of Windows [1]. It belongs to the option "Suggest ways to get the most out of Windows and finish setting up this device" in Settings > System > Notifications > Additional settings [1]. The app sets ScoobeSystemSettingEnabled to 0. Microsoft does not document the registry value behind the switch; the tutorial [1] shows the value the switch writes. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
No full screen setup prompt between signing in and the desktop.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
None; you can set up those services in Settings at any time.

## When not to use it
If you want Windows to remind you of setup steps.

## Sources
1. https://www.elevenforum.com/t/enable-or-disable-lets-finish-setting-up-your-device-in-windows-11.5205/
