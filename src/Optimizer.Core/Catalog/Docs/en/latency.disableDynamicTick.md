# Dynamic tick off (BCD)

## Summary
Expert, boot configuration: keeps the system timer ticking at a fixed rate instead of pausing it at idle. Effect disputed.

## How it works
With dynamic tick, Windows stops the periodic timer interrupt while the CPU is idle to save power. The boot option disabledynamictick yes keeps the timer running all the time [1]. The app exports the boot configuration before the change.

## Why it can help
Some users report more even frame pacing or lower DPC latency with a fixed tick.

## Evidence
Controlled tests show no consistent gaming benefit. Treat it as disputed.

## Trade-offs & risks
Higher idle power. It is a boot configuration change: if something goes wrong at boot, undo it from the recovery environment (Shift + Restart > Troubleshoot > System Restore), and the BCD export is kept in the app's data folder.

## When not to use it
Not on laptops. Only try it with physical access to the PC.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set
