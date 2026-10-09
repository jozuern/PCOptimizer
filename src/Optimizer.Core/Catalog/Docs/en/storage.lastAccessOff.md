# NTFS last-access timestamps off

## Summary
Stops NTFS from updating the last-access time of files and folders. Takes effect after a restart. No measurable gaming effect.

## How it works
NTFS records when a file or folder was last accessed. It keeps the time in memory and writes it to disk later, at most one hour later [1]. Since Windows 10 version 1803, Windows can manage this setting itself ("system managed"). The value 0x80000001 means "user managed, updates off" [2], so Windows keeps your choice. Takes effect after a restart [1].

## Why it can help
Microsoft states that turning off last-access updates speeds up file and directory access [1], because NTFS writes less metadata.

## Evidence
The saving is small metadata writes. We found no measurement that shows an effect on games.

## Trade-offs & risks
Programs that rely on last-access times, such as some backup and archiving tools, lose that information [1].

## When not to use it
Not needed on most PCs. Skip it if you use a backup or cleanup tool that works with last-access times.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
2. https://support.citrix.com/external/article/CTX338425/pvs-and-mcs-devices-cache-disk-quickly-c.html
