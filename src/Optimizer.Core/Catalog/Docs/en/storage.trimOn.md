# TRIM on

## Summary
Turns TRIM back on, so Windows tells SSDs which blocks are free. Keeps write speed up over time.

## How it works
When you delete a file, Windows only marks the space as free. TRIM passes that information to the SSD, which can then erase the blocks in the background. The value DisableDeleteNotification = 0 enables it (same as fsutil behavior set DisableDeleteNotify 0) [1].

## Why it can help
Without TRIM, the SSD has to clean up during later writes, which slows down installs and game updates.

## Evidence
Windows enables TRIM by default; this tweak only matters if it was turned off.

## Trade-offs & risks
None.

## When not to use it
Nothing to consider; keep TRIM on.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
