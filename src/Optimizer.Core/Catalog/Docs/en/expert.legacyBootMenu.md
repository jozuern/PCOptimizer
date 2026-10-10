# Classic F8 boot menu

## Summary
Brings back the Advanced options menu that opens with F8 during startup, for Safe Mode without first starting Windows. Changes the boot configuration.

## How it works
The boot configuration setting bootmenupolicy decides the boot menu type: Standard is the default on Windows 10 and later, and with Legacy the Advanced options menu (F8) is available [1]. With Standard the menu appears only in certain cases, for example after a startup failure [1]. The app sets Legacy. The menu changes with the next restart.

## Why it can help
When Windows no longer starts properly, you can reach Safe Mode with F8 instead of waiting for automatic repair.

## Evidence
Documented by Microsoft [1]. It changes only the boot menu; no effect on frame rate or latency.

## Trade-offs & risks
On PCs that start very fast the moment to press F8 is short. Boot configuration changes are Expert changes: the app exports the boot store first, and Undo sets the menu back to Standard.

## When not to use it
If you do not need Safe Mode, or your PC starts too fast to press F8.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set
