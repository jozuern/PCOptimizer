# Registry backup to the RegBack folder

## Summary
Windows copies the system registry to the RegBack folder again after each restart and on a schedule, as it did before Windows 10 1803. Uses some disk space; no effect on games.

## How it works
Since Windows 10 version 1803, Windows no longer backs up the registry hives to \Windows\System32\config\RegBack; the files there are 0 KB. Microsoft documents how to bring the old behavior back: EnablePeriodicBackup = 1 under Session Manager\Configuration Manager, then restart [1]. Windows then backs up the registry at restart and creates a RegIdleBackup task for later backups [1].

## Why it can help
A copy of the registry hives is a further way back if the registry gets damaged, besides restore points and this app's own backups.

## Evidence
Microsoft documents the setting and the backup task [1]. The article is written for Windows 10; that it works the same on Windows 11 is not confirmed by Microsoft and is part of the app's VM test plan. Microsoft recommends restore points for recovering a damaged registry [1].

## Trade-offs & risks
The copies take disk space; Microsoft turned the backup off to reduce Windows' disk footprint [1]. Restoring from RegBack is a manual recovery step from the Windows recovery environment. Undo removes the setting; the RegIdleBackup task and the copies already made may stay.

## When not to use it
If disk space is tight, or if you rely on restore points and system images anyway.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/installing-updates-features-roles/system-registry-no-backed-up-regback-folder
