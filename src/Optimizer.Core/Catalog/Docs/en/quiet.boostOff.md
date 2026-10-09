# Processor boost off (quieter and cooler)

## Summary
The processor stays at its base clock, plugged in and on battery. It runs cooler and the fans stay quieter, at the cost of peak performance.

## How it works
Boost lets the processor exceed its nominal clock when temperature and power allow it [1]. The app sets "Processor performance boost mode" to Disabled for mains and battery [1][2]. Undo writes back both previous values.

## Why it can help
Boost clocks need a higher voltage, so heat rises much faster than speed. Without boost, the processor produces clearly less heat under load, and the fans have less to do. On small cases and thin laptops that is the most direct way to lower noise.

## Evidence
The setting is documented by Microsoft [1]. How much slower the PC gets depends on the gap between base and boost clock of your processor; on many processors the boost clock is far above the base clock, so single-threaded tasks and games lose noticeably.

## Trade-offs & risks
Lower performance in games and demanding tasks. In the Gaming profiles the scan reports boost as switched off. It cannot be combined with "Processor boost off on battery" or "Restore processor boost" (same setting).

## When not to use it
For gaming, video editing or other heavy work. If noise only bothers you in games, a frame rate limit in the game is the better tool.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode
2. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
