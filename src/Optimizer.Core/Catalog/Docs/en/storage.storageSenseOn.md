# Storage Sense on

## Summary
Turns on Storage Sense. When disk space runs low, it deletes temporary files and Recycle Bin items older than 30 days.

## How it works
Storage Sense is a Windows feature that removes temporary files that are not in use and, by default, files that have been in the Recycle Bin for more than 30 days [2]. By default it runs when the drive is low on free space [1][2]. Downloads are not touched unless you turn that rule on [2]. The app turns on the switch in Settings > System > Storage; you can change the rules there. The registry value the app writes is the one behind that switch; Microsoft does not document it on its own.

## Why it can help
Keeps free space available for game updates and shader caches without manual cleanup.

## Evidence
No direct performance effect; it prevents low-disk-space problems.

## Trade-offs & risks
Files that have been in the Recycle Bin for more than 30 days are deleted for good [2]. From Windows 11 version 22H2, Microsoft says OneDrive files not opened for 30 days can be made online-only by default [1]; they stay in OneDrive but need a connection to open.

## When not to use it
Not needed if you clean up manually or keep files in the Recycle Bin on purpose.

## Sources
1. https://support.microsoft.com/en-us/windows/manage-drive-space-with-storage-sense-654f6ada-7bfc-45e5-966b-e24aded96ad5
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-storage
