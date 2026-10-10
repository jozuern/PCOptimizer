# Location access off

## Summary
Meant to turn off location services for the whole PC; a test on Windows 11 26H2 showed no effect, so it stays a Preview.

## How it works
The device-wide consent value for location is set to Deny, the value Windows stores for Settings > Privacy & security > Location > Location services, a switch only administrators can change [1]. Windows and apps then get no device location [1]. Microsoft describes the switch but does not document the registry value behind it. In a test on real Windows 11 26H2 the value had no effect: Location services stayed on in Settings and apps kept their location access, also after a restart. The tweak stays a Preview until the value behind the Settings switch is known. Microsoft's guide for managing connections from Windows names the same switch for turning off location on a device [2].

## Why it can help
No location lookups in the background. No performance effect.

## Evidence
Privacy setting; no frame rate effect.

## Trade-offs & risks
Apps, automatic time zone and Find my device lose the device location [1]. Some features, such as weather on the taskbar, can still use your IP address, and an emergency call still shares your location [1]. The app's own Wi-Fi band check shows "unknown" afterwards: Windows gives Wi-Fi details only to apps that may use the location [3].

## When not to use it
Keep location on if you use apps or features that need it.

## Sources
1. https://support.microsoft.com/en-us/windows/windows-location-service-and-privacy-3a8eee0a-5b0b-dc07-eede-2a5ca1c49088
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
3. https://learn.microsoft.com/en-us/windows/win32/nativewifi/wi-fi-access-location-changes
