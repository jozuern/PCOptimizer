# SysMain (Superfetch) off

## Summary
Disables the SysMain service, which preloads frequently used programs into memory. Only offered on PCs with SSDs only. Effect disputed.

## How it works
SysMain (formerly Superfetch) is a Windows service that, according to its description, maintains and improves system performance over time [2]. It preloads data of frequently used programs into free memory. Microsoft wrote in 2009 that Superfetch and the related preloading features were designed for hard disks and that Windows 7 turns them off on fast SSDs [1]. The app sets the start type of the service to Disabled. The service stops at the next restart.

## Why it can help
Saves the background disk and CPU activity of preloading.

## Evidence
We found no measurement from Microsoft or a hardware vendor that shows a gaming difference on SSD-only PCs, in either direction.

## Trade-offs & risks
Programs may start a little slower the first time after boot. Microsoft does not document which other memory features rely on SysMain.

## When not to use it
Not on PCs with a hard disk, where preloading was designed to help [1].

## Sources
1. https://learn.microsoft.com/en-us/archive/blogs/e7/support-and-qa-for-solid-state-drives
2. https://learn.microsoft.com/en-us/windows-server/security/windows-services/security-guidelines-for-disabling-system-services-in-windows-server
