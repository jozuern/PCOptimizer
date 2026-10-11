# Location access off

## Summary
Turns off location services for the whole PC with the policy "Turn off location". Settings then shows the switch off and locked. For Pro, Enterprise and Education.

## How it works
The app sets the Microsoft policy "Turn off location" (DisableLocation = 1), which Microsoft names for turning off location on a device [2][4]. Windows and apps then get no device location [1]. In Settings > Privacy & security > Location, Location services and every app permission show off and cannot be changed while the policy is set. A test on Windows 11 26H2 showed this right after apply, and Location services on again after undo. Writing the consent value that Settings stores no longer works on Windows 11 26H2: Windows keeps the switch in its own store and ignores that value. Home editions are left out because the test ran on Pro.

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
4. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-sensors
