# Hibernation off

## Summary
Turns hibernation off and deletes hiberfil.sys, whose size is a share of your RAM, so it frees that disk space. Also disables Fast Startup.

## How it works
The app runs powercfg /hibernate off [1]. Windows deletes the hibernation file and removes Hibernate and Fast Startup. Undo runs powercfg /hibernate on.

## Why it can help
Frees the space of the hibernation file on the system drive, which helps when space is short. There is no performance effect.

## Evidence
No frame-rate effect.

## Trade-offs & risks
No more Hibernate option and no Fast Startup. Laptops with Modern Standby hibernate after a set battery drain during sleep, so the battery is not empty when you open the lid [2]; the tweak is blocked there. Undo turns hibernation back on with the Windows default file size; a custom size is not restored.

## When not to use it
Do not use on laptops with Modern Standby, or if you use Hibernate.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options
2. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/adaptive-hibernate
