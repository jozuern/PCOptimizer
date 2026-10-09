# TRIM on

## Summary
Turns TRIM back on, so Windows tells SSDs which blocks are free. Windows has it on by default; this only matters if it was turned off.

## How it works
When you delete a file, Windows only marks the space as free. TRIM passes that information to the SSD, which can then erase the blocks in the background. The value DisableDeleteNotification = 0 enables it (same as fsutil behavior set DisableDeleteNotify 0) [1].

## Why it can help
With TRIM, the SSD learns which blocks are free right after a delete and can prepare them in the background. Without it, the drive only finds out when the blocks are overwritten.

## Evidence
NTFS has TRIM on by default unless an administrator turns it off [1]. Changing it needs no restart [1].

## Trade-offs & risks
Microsoft notes that some devices may slow down with delete notifications on [1]; that is the only documented reason to turn TRIM off. If a group policy turns TRIM off for all volumes [2], this setting has no effect until the policy is removed.

## When not to use it
Only if your drive maker tells you to turn TRIM off for that drive.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-filesys
