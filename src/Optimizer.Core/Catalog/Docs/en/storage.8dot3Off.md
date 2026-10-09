# 8.3 short file names off

## Summary
Stops NTFS from creating DOS-style short names (like PROGRA~1) for new files. Small effect; existing short names stay.

## How it works
For compatibility with very old programs, NTFS can create an 8.3 short name for every file. The value NtfsDisable8dot3NameCreation = 1 turns that off on all volumes [1]. Existing short names are not removed, because removing them can break installers.

## Why it can help
Creating many files (game installs, shader caches) does slightly less work.

## Evidence
No measurable gaming effect on SSDs; the gain is in folders with very many files.

## Trade-offs & risks
Very old 16-bit era programs that need short names may fail on new files.

## When not to use it
Not needed on most PCs.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
