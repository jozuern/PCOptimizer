# Drivers not included with Windows Update

## Summary
Windows Update stops installing drivers with quality updates, so a graphics driver you installed yourself is not replaced. You then update all drivers yourself.

## How it works
By default, Windows Update also offers updates with the Driver classification. The app turns on the documented policy "Do not include drivers with Windows Updates" (Computer Configuration > Windows Components > Windows Update > Manage updates offered from Windows Update): ExcludeWUDriversInQualityUpdate = 1 under the Windows Update policy key [1]. Windows reads it at the next update scan.

## Why it can help
If you install graphics drivers from NVIDIA, AMD or Intel yourself, Windows Update can no longer replace them with an older or different version, which keeps the driver you chose and its settings.

## Evidence
Microsoft documents what the policy does, not an effect on games [1]. The benefit is predictability: the driver changes only when you change it.

## Trade-offs & risks
It applies to all drivers, not only the graphics driver: chipset, network, audio and other driver updates from Windows Update stop as well, and you have to install them from the manufacturers' pages (the Apps & drivers page helps). Laptops that get their drivers through Windows Update are affected most. Microsoft lists the policy for Pro, Enterprise and Education, so the app does not offer it on Home.

## When not to use it
If you do not update drivers yourself, or on a laptop whose manufacturer delivers drivers through Windows Update.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
