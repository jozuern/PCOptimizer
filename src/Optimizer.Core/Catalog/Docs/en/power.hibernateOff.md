# Hibernation off

## Summary
Turns hibernation off and deletes hiberfil.sys, which frees disk space roughly the size of a large part of your RAM. Also disables Fast Startup.

## How it works
The app runs powercfg /hibernate off [1]. Windows deletes the hibernation file and removes Hibernate and Fast Startup. Undo runs powercfg /hibernate on.

## Why it can help
Frees several gigabytes on the system drive, which helps when space is short. There is no performance effect.

## Evidence
No frame-rate effect.

## Trade-offs & risks
No more Hibernate option and no Fast Startup. Laptops with Modern Standby use hibernation to protect the battery during long sleep, so it is blocked there.

## When not to use it
Do not use on laptops with Modern Standby, or if you use Hibernate.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options
