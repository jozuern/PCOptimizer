# Power throttling off

## Summary
Stops Windows from running background processes in a power-saving mode. Mainly relevant on laptops and CPUs with efficiency cores.

## How it works
With power throttling, Windows classifies background work as EcoQoS and tries to run it more efficiently, for example at a lower clock or on efficiency cores [1]. The value PowerThrottlingOff = 1 is the registry value of the Windows policy "Turn off Power Throttling" and turns this off system-wide [2]. With the Windows power mode set to Best performance, Windows already opts all apps out of power throttling [3].

## Why it can help
Tools that run next to a game, such as voice chat, overlays or streaming software, may otherwise be slowed down when they lose focus.

## Evidence
On desktops without efficiency cores the effect is usually not measurable. On hybrid CPUs and laptops, background apps keep full speed.

## Trade-offs & risks
Higher power draw, especially on battery. The app asks for a restart after applying it.

## When not to use it
Not useful on desktops without efficiency cores. On laptops, consider the battery cost.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-power
3. https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/customize-power-slider
