# Reserved storage off

## Summary
Gives back the space Windows keeps reserved for updates, temporary files and caches. On small drives updates may then fail for lack of space.

## How it works
Windows reserves part of the drive so updates can be downloaded and installed without you freeing space, and uses it for temporary files and caches meanwhile [1][2]. The app turns it off with DISM /Set-ReservedStorageState /State:Disabled [1]. DISM refuses while an update is using the space; try again later then [1]. Undo turns it back on.

## Why it can help
On a small system drive the reserved space can be the difference between a game update fitting or not.

## Evidence
Documented by Microsoft [1][2]. You see the size under Settings > System > Storage > System & reserved [2]; it changes no performance.

## Trade-offs & risks
Windows updates need free space again: with a nearly full drive they can fail until you free space or undo the tweak. Microsoft's own guidance for managed PCs is to turn it off only around an update [1].

## When not to use it
If the drive has plenty of free space, or you do not check free space before big Windows updates.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-storage-reserve?view=windows-11
2. https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/storage-settings-in-windows
