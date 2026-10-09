# Power throttling off

## Summary
Stops Windows from running background processes in a power-saving mode. Mainly relevant on laptops and CPUs with efficiency cores.

## How it works
With power throttling, Windows marks background work as low priority and runs it at efficient clocks or on efficiency cores (EcoQoS) [1]. The value PowerThrottlingOff = 1 turns this off for all processes.

## Why it can help
Tools that run next to a game, such as voice chat, overlays or streaming software, may otherwise be slowed down when they lose focus.

## Evidence
On desktops without efficiency cores the effect is usually not measurable. On hybrid CPUs and laptops, background apps keep full speed.

## Trade-offs & risks
Higher power draw, especially on battery. Takes effect after a restart.

## When not to use it
Not useful on desktops without efficiency cores. On laptops, consider the battery cost.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
