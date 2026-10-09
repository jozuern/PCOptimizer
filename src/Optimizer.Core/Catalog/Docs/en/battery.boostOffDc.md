# Processor boost off on battery

## Summary
On battery, the processor stays at its base clock instead of boosting above it. Plugged in, nothing changes.

## How it works
Boost lets the processor run above its nominal clock when temperature and power allow it [1]. The app sets the power plan value "Processor performance boost mode" for battery to Disabled [1][2]. The setting is normally hidden in Power Options [1]. The value for mains power stays as it is.

## Why it can help
Boost clocks need a higher voltage, so energy use rises more than speed. Without boost, short bursts (opening apps, loading web pages) use less energy, the laptop stays cooler and the fan runs less often. For writing, browsing and video this usually means longer runtime.

## Evidence
The setting is documented by Microsoft [1]. How much runtime it gains depends on the processor and on what you do: light work with many short bursts gains more than video playback, which hardly boosts at all.

## Trade-offs & risks
On battery, demanding tasks (exports, compiling, games) take noticeably longer, because the processor cannot exceed its base clock. Undo restores the previous battery value.

## When not to use it
If you regularly do heavy work on battery and need full speed there. Games on battery are slow anyway; plug in for gaming.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode
2. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
