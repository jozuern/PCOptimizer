# Graphics card PCIe link width

## Summary
::: variant slotLimited
{{gpu}} runs with fewer PCIe lanes than it supports because the slot offers fewer lanes. Usually it sits in the wrong slot or shares lanes.
:::
::: variant trainedDown
{{gpu}} runs with fewer PCIe lanes than it supports. Typical causes are the slot, a riser cable, a card that is not fully seated or shared lanes.
:::
::: variant designLimited
{{gpu}} runs with fewer PCIe lanes than its maximum. In laptops the graphics chip can be wired with fewer lanes by design.
:::
::: variant default
Checks that the graphics card uses all the PCIe lanes it supports, for example x16.
:::

## Why it matters
The graphics card exchanges data with the processor over PCIe lanes. A card made for x16 that runs at x8 or x4 has half or a quarter of the bandwidth. How much that costs depends on the game, the PCIe generation and how much data the game moves; this app does not measure it. Cards built with fewer lanes, for example x8, are compared with their own maximum, so they are not reported.
::: variant designLimited
In a laptop the lanes are fixed by the maker, so there is nothing to change.
:::

## How we detected it
We read the current (negotiated) and maximum PCIe link width of the graphics card and the maximum of the slot (port) above it from Windows. The link width is the number of lanes the link uses [1]. Some cards (AMD Radeon RX 5000 and newer) contain a PCIe switch; we skip it to reach the link between card and slot. Only the lane count is judged, because the PCIe generation can change with load. Some boards do not report the slot's maximum; then the cause cannot be narrowed down.

## How to fix
::: variant slotLimited
1. Check the board manual for the slot connected to the CPU with x16 lanes, usually the top long slot.
2. Move the card there.
3. Check the manual for lane sharing as well: on some boards, using certain M.2 slots splits the graphics card slot to x8.
:::
::: variant trainedDown
1. Shut down, unplug, and reseat the card firmly; make sure the slot latch closes.
2. If you use a riser cable (vertical mount), try without it or with a riser for the right PCIe generation.
3. Check the board manual for lane sharing: some M.2 slots take lanes from the graphics card slot.
:::
::: variant designLimited,default
No action needed.
:::

## How to check the fix
Run the scan again. The current width should equal the card's maximum. GPU-Z also shows the bus interface (it has a load test to show the generation under load).

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_pci_express_link_status_register
