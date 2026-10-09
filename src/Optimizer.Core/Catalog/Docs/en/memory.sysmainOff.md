# SysMain (Superfetch) off

## Summary
Disables the SysMain service, which preloads frequently used programs into memory. Only offered on PCs with SSDs only. Effect disputed.

## How it works
SysMain watches which programs you use and loads them into free memory ahead of time; it was designed for hard disks [1]. On SSDs, programs load fast without it. The app sets the service start type to Disabled.

## Why it can help
Saves the background disk and CPU activity of preloading.

## Evidence
On SSD-only systems, measurements show no consistent difference either way.

## Trade-offs & risks
Programs may start a little slower the first time after boot.

## When not to use it
Not on PCs with a hard disk, where SysMain still helps.

## Sources
1. https://learn.microsoft.com/en-us/powershell/module/mmagent/disable-mmagent
