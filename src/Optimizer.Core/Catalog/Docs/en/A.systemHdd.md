# Windows is installed on a hard disk

## Summary
::: status Problem
Windows runs from the hard disk {{disk}}. Startup, updates and loading in games are much slower than from an SSD.
:::
::: status Ok,Unknown,Info,Unsupported
Checks whether Windows is installed on an SSD.
:::

## Why it matters
Windows reads many small files all the time: at startup, for updates, for the page file, for shader caches and when games load. A hard disk has to wait for the platter to turn before each random access: at 7200 rpm one turn takes 8.3 ms, so the average wait is about 4.2 ms before the head even moves [1]. An SSD has no moving parts and needs a fraction of that. With Windows on a hard disk, games can stutter whenever the system accesses the disk in the background.

## How we detected it
We find the disk that holds the Windows volume and read its media type from the Windows storage management interface.

## How to fix
1. Add an SSD (NVMe if the board has an M.2 slot, otherwise SATA).
2. Move Windows: either install it fresh on the SSD (cleanest), or clone the system with the SSD manufacturer's migration tool.
3. **BitLocker:** if BitLocker or device encryption is on, suspend protection before you clone or change the boot drive (Start > **Manage BitLocker** > **Suspend protection**), or have the recovery key ready; it is often saved in your Microsoft account. A change to the boot configuration can make Windows ask for it at the next start [2][3].
4. Afterwards, set the SSD as the first boot drive in the BIOS. Keep the hard disk for files and media.

## How to check the fix
Run the scan again. The Windows disk should show the media type "SSD".

## Sources
1. https://www.seagate.com/files/www-content/product-content/barracuda-fam/barracuda-new/en-us/docs/100817550m.pdf
2. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
3. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
