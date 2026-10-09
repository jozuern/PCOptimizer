# Integrated graphics enabled but unused

## Summary
::: status Info
The integrated graphics ({{igpu}}) are enabled but no display uses them. That is fine; it does not cost frame rate.
:::
::: status Ok,Unknown,Problem,Unsupported
Checks whether the processor's integrated graphics are enabled on a desktop with a graphics card.
:::

## Why it matters
With a graphics card installed, the integrated graphics are idle. They reserve a small amount of memory and are useful for hardware video encoding (for example Intel Quick Sync in OBS or video editors) and for extra monitor outputs. Rarely, a game or program picks the wrong GPU when both are present.

## How we detected it
We check that both an integrated and a dedicated GPU are active and that no display is connected to the integrated one.

## How to fix
1. Keep it enabled if you use hardware video encoding or might need another display output.
2. If a game uses the wrong GPU, set it to the high performance GPU in **Settings > System > Display > Graphics** instead of disabling the integrated graphics.
3. Only if you do not need it at all: disable it in the BIOS (often "iGPU Multi-Monitor" or "Integrated Graphics" set to Disabled, with the primary display set to PCIe).

## How to check the fix
No check needed; this is information.
