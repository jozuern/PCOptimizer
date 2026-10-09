# Fast Startup off

## Summary
Makes "Shut down" a real shutdown again. Drivers start fresh on the next boot instead of being restored from a hibernation file.

## How it works
With Fast Startup, shutting down closes all apps and signs out all users, then saves the kernel and the loaded drivers to the hibernation file and restores them at the next start [1]. The app sets HiberbootEnabled to 0, the documented value for turning Fast Startup off [2]. Without it, Windows initializes all drivers from scratch on every boot. If your organization enforces Fast Startup by policy, the policy wins [3].

## Why it can help
Problems that build up in a driver (for example after a GPU driver update) are cleared by a normal shutdown, not only by Restart. Dual boot and BIOS changes also behave more predictably.

## Evidence
No frame-rate effect. A cold start takes longer than a fast startup [1]; how much depends on the PC.

## Trade-offs & risks
Slower cold boot.

## When not to use it
Not needed if you rarely shut down, or if boot time matters more to you than a clean driver state.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/distinguishing-fast-startup-from-wake-from-hibernation
2. https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/hibernate-once-resume-many-horm
3. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-wininit
