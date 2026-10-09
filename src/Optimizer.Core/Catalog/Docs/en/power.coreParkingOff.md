# Core parking off

## Summary
Keeps all processor cores unparked on mains power. Often recommended, but tests show no consistent gaming benefit on current CPUs.

## How it works
Core parking lets Windows put idle cores into a deep sleep state and route threads to fewer cores. The setting CPMINCORES sets the minimum share of cores that stay unparked [1]; 100 % disables parking.

## Why it can help
When a game suddenly needs more threads, parked cores first have to wake up. Without parking, that wake-up delay disappears.

## Evidence
On current Intel and AMD desktop CPUs, measured differences are usually within run-to-run variance. Windows already unparks cores quickly under load.

## Trade-offs & risks
Higher idle power. On Ryzen X3D processors with two chiplets it is blocked: the AMD driver relies on parking to keep games on the V-Cache chiplet.

## When not to use it
Do not use on multi-chiplet Ryzen X3D CPUs. On laptops and hybrid Intel CPUs (P- and E-cores) it is not recommended.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
