# Notifications without sound

## Summary
Notifications still appear, but no longer play a sound, for all apps at once.

## How it works
Settings lets you turn the sound of notifications on or off [1]. The app sets the global value NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND to 0, which covers all apps. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
No notification sounds over game audio or voice chat.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
You may miss notifications when you are not looking at the screen.

## When not to use it
If you rely on sounds for reminders or messages.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/notifications-and-do-not-disturb-in-windows
