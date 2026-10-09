# Remove forced platform clock (useplatformclock)

## Summary
Expert, boot configuration: removes useplatformclock, a debugging option that older guides set. It forces a slower platform timer; removing it lets Windows pick its timer again.

## How it works
The boot option useplatformclock forces Windows to use the platform clock as its performance counter; Microsoft says it should only be used for debugging [1]. The platform clock is the HPET or the ACPI PM timer. Normally Windows uses the CPU's time stamp counter (TSC) when it is suitable [2]. The app deletes the option after exporting the boot configuration.

## Why it can help
Reading the TSC takes tens to a few hundred CPU cycles. Reading a platform timer takes about 0.8 to 1.0 microseconds and needs a system call [2]. Games query the time many times per frame, so the difference adds up.

## Evidence
The cost difference is documented by Microsoft [2]. How much it changes the frame rate depends on how often a game queries the time.

## Trade-offs & risks
A boot configuration change. If BitLocker is on, keep your recovery key at hand: Microsoft notes that BitLocker may need to be suspended before changing boot options [1]. If the PC does not start correctly, undo it from the recovery environment (Shift + Restart > Troubleshoot > System Restore); the BCD export is kept in the app's data folder.

## When not to use it
Nothing to do if the option is not set. Only change boot settings with physical access to the PC.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set
2. https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps
