# No automatic device encryption

## Summary
Stops Windows from turning on BitLocker device encryption by itself. It does not decrypt a drive that is already encrypted.

## How it works
Device encryption turns on BitLocker automatically on eligible PCs after setup; since Windows 11 24H2 more PCs are eligible [1]. Microsoft lists PreventDeviceEncryption = 1 under HKLM\SYSTEM\CurrentControlSet\Control\BitLocker to prevent it [1].

## Why it can help
Useful before you reinstall or move a drive, if you do not want an encrypted drive whose recovery key you might not have.

## Evidence
Documented by Microsoft [1]. On PCs where encryption is already on, nothing changes; check in Settings > Privacy & security > Device encryption.

## Trade-offs & risks
An unencrypted drive can be read by anyone who takes it out of the PC. To encrypt later, undo the tweak and turn on device encryption or BitLocker.

## When not to use it
On a laptop that leaves the house, or whenever the drive holds data that must stay private.

## Sources
1. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/
