# Windows is installed on a hard disk

## Summary
::: status Problem
Windows runs from the hard disk {{disk}}. Startup, updates and loading in games are much slower than from an SSD.
:::
::: status Ok,Unknown,Info,Unsupported
Checks whether Windows is installed on an SSD.
:::

## Why it matters
Windows reads many small files all the time: at startup, for updates, for the page file, for shader caches and when games load. A hard disk needs a few milliseconds per random access, an SSD a fraction of that. With Windows on a hard disk, games can stutter whenever the system accesses the disk in the background.

## How we detected it
We find the disk that holds the Windows volume and read its media type from the Windows storage management interface.

## How to fix
1. Add an SSD (NVMe if the board has an M.2 slot, otherwise SATA).
2. Move Windows: either install it fresh on the SSD (cleanest), or clone the system with the SSD manufacturer's migration tool.
3. Afterwards, set the SSD as the first boot drive in the BIOS. Keep the hard disk for files and media.

## How to check the fix
Run the scan again. The Windows disk should show the media type "SSD".
