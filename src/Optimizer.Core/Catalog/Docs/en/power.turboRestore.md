# Restore processor turbo

## Summary
Sets the maximum processor state to 100 % and turbo boost to Aggressive in the active plan, so the CPU can reach its turbo clock again.

## How it works
Two values of the active power plan control turbo: the maximum processor state (PROCTHROTTLEMAX) and the boost mode (PERFBOOSTMODE). Below 100 %, or with boost disabled, Windows does not request turbo frequencies [1]. The app sets both for mains power.

## Why it can help
Current CPUs run far above their base clock in games. With turbo blocked, CPU-limited games lose a large share of their frame rate.

## Evidence
The loss without turbo is easy to measure: the CPU clock in Task Manager stays at or below the base clock under load.

## Trade-offs & risks
Higher power draw and temperature under load, which is the normal behavior of the CPU. On a laptop on battery, the battery setting is not touched.

## When not to use it
Only needed when the scan reports turbo as disabled. If you limited turbo on purpose (for example for noise), keep your setting.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode
