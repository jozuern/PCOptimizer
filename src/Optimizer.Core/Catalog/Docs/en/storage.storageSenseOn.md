# Storage Sense on

## Summary
Turns on Storage Sense, which deletes temporary files and empties the Recycle Bin automatically when space runs low.

## How it works
Storage Sense is a Windows feature that removes temporary files and, depending on your settings, old Recycle Bin and Downloads content [1]. The app turns it on with Windows' default rules; you can adjust them in Settings > System > Storage.

## Why it can help
Keeps free space available for game updates and shader caches without manual cleanup.

## Evidence
No direct performance effect; it prevents low-disk-space problems.

## Trade-offs & risks
With the Downloads rule enabled in Settings, old files in Downloads can be deleted. The default does not touch Downloads.

## When not to use it
Not needed if you clean up manually.

## Sources
1. https://support.microsoft.com/en-us/windows/manage-drive-space-with-storage-sense-654f6ada-7bfc-45e5-966b-e24aded96ad5
