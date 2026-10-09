# Remove forced HPET (useplatformclock)

## Summary
Expert, boot configuration: removes useplatformclock, which older guides set and which forces the slow HPET timer. Removing it restores Windows' default timer.

## How it works
The boot option useplatformclock forces Windows to use the platform clock (HPET) as its time source [1]. Reading HPET is much slower than the CPU's own timer, so programs that query time often (games do) spend more time in timer calls. The app deletes the option after exporting the boot configuration.

## Why it can help
Games that read the time every frame run with less overhead; frame pacing can improve.

## Evidence
The extra cost of HPET reads is well documented; the effect on frame rate depends on how often a game queries time.

## Trade-offs & risks
A boot configuration change. If the PC does not start correctly, undo it from the recovery environment (Shift + Restart > Troubleshoot > System Restore); the BCD export is kept in the app's data folder.

## When not to use it
Nothing to do if the option is not set. Only change boot settings with physical access to the PC.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set
