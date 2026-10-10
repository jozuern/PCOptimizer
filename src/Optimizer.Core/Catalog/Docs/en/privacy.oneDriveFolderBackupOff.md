# OneDrive: no backup of Desktop, Documents and Pictures

## Summary
OneDrive can no longer move your Desktop, Documents and Pictures folders into OneDrive, and it stops asking you to. Folders already moved stay in OneDrive.

## How it works
The OneDrive policy "Prevent users from moving their Windows known folders to OneDrive" (KFMBlockOptIn = 1) blocks moving these folders to any OneDrive account; users are not prompted to protect their folders and the Manage backup command is disabled [1]. Folders that were already moved stay in OneDrive [1].

## Why it can help
Your Desktop, Documents and Pictures stay on this PC and are not uploaded by a click on a prompt.

## Evidence
A documented OneDrive policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
No automatic cloud backup of these folders; make your own backups. Folders already in OneDrive have to be moved back in OneDrive settings.

## When not to use it
If you want OneDrive to back up these folders.

## Sources
1. https://learn.microsoft.com/en-us/sharepoint/use-group-policy
