# Copy to new accounts

## Summary
Writes the per-user tweaks you turned on into the Default profile, so accounts created later start with them. Accounts that exist already keep their own settings.

## How it works
Windows uses the Default profile as the template for every new account [1]. Tunero loads its registry hive (C:\Users\Default\NTUSER.DAT) under HKEY_USERS, as Microsoft's own steps do with Load Hive [1][3], writes the values of your tweaks there and unloads it again. Only tweaks made entirely of registry values in your account go along: a tweak that also calls Windows or changes the whole PC would arrive half done. The original values are kept, and Undo on the Changes page puts them back.

## Why it can help
After a reinstall or on a family PC, a new account gets the same Explorer, taskbar and privacy settings without turning each tweak on again while signed in as that account.

## Evidence
There is no speed effect: it saves setup work. A VM test showed a new local account starting with the copied values.

## Trade-offs & risks
Microsoft names Sysprep with CopyProfile as the supported way to prepare the Default profile for images [2]; writing single values into the hive is the step its CopyProfile page shows by hand [1]. Windows resets some settings at the first sign-in of a new account, and treats settings protected by a per-user hash (such as default apps) as invalid and resets them [1]. Undoing a tweak in your account later does not change the Default profile: undo this change for that.

## When not to use it
On a PC where only you sign in, or when new accounts should start with Windows defaults. In an organization, the Default profile is usually managed through the image or Group Policy.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/customize-the-default-user-profile-by-using-copyprofile
2. https://learn.microsoft.com/en-us/troubleshoot/windows-client/setup-upgrade-and-drivers/customize-default-local-user-profile
3. https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regloadkeyw
