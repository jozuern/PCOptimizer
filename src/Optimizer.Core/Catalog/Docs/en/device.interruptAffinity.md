# Interrupt affinity for this device (Expert)

## Summary
Routes this device's interrupts to one specific processor core instead of letting Windows choose. Disputed: can help or hurt, measure it.

## How it works
Windows normally spreads device interrupts over the processor cores. The interrupt affinity policy in the registry [1] can tell it to send them to specific cores. The app sets the documented policy "specified processors" and a mask with one logical processor (shown in the name), away from core 0 and 1 where Windows handles most system work. Takes effect after a restart.

## Why it can help
Keeping interrupts off the cores where the game's main thread runs can reduce delay spikes in some setups.

## Evidence
Results differ between systems and games; there is no consistent measured gain. Use the benchmark on the Health page before and after.

## Trade-offs & risks
If the chosen core is busy, interrupts wait longer, which can make things worse. A wrong setting for a critical device can affect stability, so this is an Expert, boot-critical change.

## When not to use it
Without a before and after measurement, or on processors with few cores.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/interrupt-affinity-and-priority
