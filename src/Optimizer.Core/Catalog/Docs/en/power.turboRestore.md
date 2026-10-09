# Restore processor turbo

## Summary
Sets the maximum processor state to 100 % and the boost mode to the Windows default in the active plan, so the CPU can reach its boost clock again.

## How it works
Two values of the active power plan control boost: the maximum processor state (PROCTHROTTLEMAX), a percentage of the maximum processor performance [2], and the boost mode (PERFBOOSTMODE). With boost mode Disabled, the processor does not go above its nominal performance level [1]; a maximum state below 100 % caps the performance Windows requests. The app sets both for mains power to 100 % and to boost mode 2, which is what the built-in Windows plans use. Depending on the processor, Windows shows that value as Aggressive or Enabled [1].

## Why it can help
Current CPUs run above their base clock in games. With boost blocked, games that are limited by the CPU lose frame rate; games limited by the graphics card lose less.

## Evidence
How much is lost depends on the gap between base and boost clock and on whether the game is limited by the CPU. You can check it: with boost blocked, the clock in Task Manager does not rise above the base speed under load.

## Trade-offs & risks
Higher power draw and temperature under load, which is the normal behavior of the CPU. On a laptop on battery, the battery setting is not touched.

## When not to use it
Only needed when the scan reports turbo as disabled. If you limited turbo on purpose (for example for noise), keep your setting.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode
2. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-maxperformance
