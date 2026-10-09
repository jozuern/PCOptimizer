# NTFS last-access timestamps off

## Summary
Stops NTFS from updating the last-access time of files on every read. Windows already does this on large volumes; small effect.

## How it works
NTFS can store when a file was last read, which costs an extra metadata write per access. The value 0x80000001 turns updates off for all volumes and keeps the choice fixed [1].

## Why it can help
Fewer small metadata writes during heavy file access, such as loading many small game files.

## Evidence
Since Windows 10 1803, Windows turns last-access updates off automatically on large volumes, so the default is usually already off. No measurable gaming effect.

## Trade-offs & risks
Tools that rely on last-access times (some backup or cleanup tools) lose that information.

## When not to use it
Not needed on most PCs.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
