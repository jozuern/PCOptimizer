# No wake timers

## Summary
Scheduled tasks, maintenance and updates can no longer wake the PC from sleep. Useful when a PC turns itself on at night. Applies to the active power plan.

## How it works
The power setting "Allow wake timers" decides whether the system uses its wake-on-timer capability, for example to wake up and install updates [1]. The app sets it to 0 (disabled) for mains and battery on the active power plan [1]. Windows otherwise schedules a Regular Maintenance task at 3 AM that can wake the PC through such a timer [2].

## Why it can help
The PC stays asleep at night: no fans, no noise, no battery drain in a bag.

## Evidence
A documented power setting [1][2]. No effect on frame rate or latency.

## Trade-offs & risks
Maintenance and updates run when you use the PC next, which can mean a little more activity right after waking. Tasks you set to wake the PC, such as a recording or a backup, no longer wake it.

## When not to use it
If you rely on scheduled tasks that wake the PC, for example for backups or recordings.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/sleep-settings-automatically-wake-for-tasks
2. https://learn.microsoft.com/en-us/troubleshoot/windows-client/setup-upgrade-and-drivers/desktop-wakes-up-unexpectedly-from-sleep-hibernation
