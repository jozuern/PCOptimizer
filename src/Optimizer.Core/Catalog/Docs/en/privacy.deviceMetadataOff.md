# No automatic download of device apps

## Summary
Windows no longer downloads the manufacturer apps that belong to devices you connect, such as mouse or headset software. Drivers still install. No effect on performance.

## How it works
When you connect a device, Windows can download applications associated with the device's metadata. The app turns on the documented policy "Prevent automatic download of applications associated with device metadata" (Computer Configuration > System > Device Installation): PreventDeviceMetadataFromNetwork = 1 [1]. It overrides the setting in the Device Installation Settings dialog box.

## Why it can help
Vendor apps that you did not ask for are not installed in the background. You install the tools you want yourself.

## Evidence
Microsoft documents that Windows then does not download these applications [1]. There is no measurement of an effect on games, and none is expected.

## Trade-offs & risks
A new mouse, keyboard or headset does not get its vendor app automatically; install it yourself if you need its features (lighting, button mapping, equalizer). Microsoft lists the policy for Pro, Enterprise and Education, so the app does not offer it on Home.

## When not to use it
If you want vendor apps to install by themselves when you connect a device.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-deviceinstallation
