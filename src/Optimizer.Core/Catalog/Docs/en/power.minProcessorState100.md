# Minimum processor state 100 %

## Summary
Keeps the processor at its full clock even at idle. Often suggested, but no consistent gaming benefit on current CPUs.

## How it works
The minimum processor state (PROCTHROTTLEMIN) is the lowest performance level Windows requests from the CPU [1]. At 100 %, Windows never asks for a lower clock. Modern CPUs still enter idle states, but they wake into full clock.

## Why it can help
In theory, the CPU skips the ramp-up when load arrives.

## Evidence
Current CPUs ramp up within milliseconds. Gaming benchmarks rarely show differences beyond run-to-run variance.

## Trade-offs & risks
Noticeably higher idle power, heat and fan noise.

## When not to use it
Not useful on laptops or when idle noise matters. Prefer a performance power plan instead.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
