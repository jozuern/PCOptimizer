# Interrupt affinity for this device (Expert)

## Summary
Routes this graphics card's interrupts to one logical processor instead of the Windows default policy. Disputed: can help or hurt, measure it.

## How it works
Windows normally lets the device's driver and its own default policy decide which processors handle a device's interrupts. The interrupt affinity policy in the registry can override that [1]. The app sets the documented policy "specified processors" and a mask with one logical processor (shown in the name), away from the first cores. Takes effect after a restart. The app offers this only for graphics cards.

## Why it can help
Keeping interrupts off the cores where the game's main thread runs can reduce delay spikes in some setups.

## Evidence
Results differ between systems and games; there is no consistent measured gain. Use the benchmark on the Health page before and after.

## Trade-offs & risks
Microsoft recommends the default policy where it fits [1]. If the chosen processor is busy, interrupts wait longer, which can make things worse. A wrong setting for an important device can affect stability, so this is an Expert, boot-critical change.

## When not to use it
Without a before and after measurement, or on processors with few cores.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/interrupt-affinity-and-priority
