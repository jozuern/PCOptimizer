# Dark mode for Windows and apps

## Summary
Switches Windows and apps that follow it to dark mode, like Settings > Personalization > Colors > Choose your mode > Dark.

## How it works
Windows has a light and a dark color mode for Windows and for apps [1]. The app sets AppsUseLightTheme and SystemUsesLightTheme to 0 and tells running programs that the setting changed. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
Easier on the eyes in a dark room and matches dark game launchers.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
Some older programs keep their light look.

## When not to use it
If you prefer the light mode.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/personalize-your-colors-in-windows
