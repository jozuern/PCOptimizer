# Graphics card PCIe link width

## Summary
::: variant slotLimited
{{gpu}} runs with fewer PCIe lanes than it supports because the slot offers fewer lanes. Usually it sits in the wrong slot.
:::
::: variant trainedDown
{{gpu}} runs with fewer PCIe lanes than it supports. Typical causes are the slot, a riser cable, a card that is not fully seated or shared lanes.
:::
::: variant default
Checks that the graphics card uses all the PCIe lanes it supports, for example x16.
:::

## Why it matters
The graphics card exchanges data with the processor over PCIe lanes. A card made for x16 that runs at x8 or x4 has half or a quarter of the bandwidth. At x8 with PCIe 4.0 or newer the loss is usually small; at x4, or with older PCIe generations, games lose clearly measurable performance, especially when they stream a lot of data or run out of video memory. Cards built with x8 (for example RTX 4060/5060 class) are normal at x8.

## How we detected it
We read the PCIe link properties of the graphics card and of the slot (port) above it from Windows. Some cards (AMD Radeon RX 5000 and newer) contain a PCIe switch; we skip it to reach the link between card and slot. Only the lane count is judged. The PCIe generation drops at idle to save power (Gen 1 at the desktop is normal) and is only meaningful under load. Some boards do not report the slot's maximum; then the cause cannot be narrowed down.

## How to fix
::: variant slotLimited
1. Check the board manual for the slot connected to the CPU with x16 lanes, usually the top long slot.
2. Move the card there.
:::
::: variant trainedDown
1. Shut down, unplug, and reseat the card firmly; make sure the slot latch closes.
2. If you use a riser cable (vertical mount), try without it or with a riser for the right PCIe generation.
3. Check the board manual for lane sharing: some M.2 slots take lanes from the graphics card slot.
:::
::: variant default
No action needed.
:::

## How to check the fix
Run the scan again. The current width should equal the card's maximum. GPU-Z also shows the bus interface (it has a load test to show the generation under load).

## Sources
1. https://docs.nvidia.com/deploy/nvml-api/group__nvmlDeviceQueries.html
