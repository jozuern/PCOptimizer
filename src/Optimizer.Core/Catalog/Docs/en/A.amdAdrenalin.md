# AMD Software settings for gaming

## Summary
::: status Info
Settings in AMD Software: Adrenalin Edition that matter for gaming on {{gpu}}. The app explains them; they are changed in AMD Software.
:::
::: status Ok,Unknown,Problem,Unsupported
Recommended AMD Software settings for gaming.
:::

## Why it matters
Some Radeon features trade image quality or frame rate for power savings or smoothness, and their defaults depend on the driver version and the preset you chose at installation. AMD's settings library (ADLX) can change these values [1], but it is a native library that this app does not include, so the app explains the settings instead of changing them.

## How we detected it
We found a Radeon graphics card with an AMD driver. Workstation cards (Radeon Pro, FirePro, Instinct) are skipped, because these steps are written for AMD Software: Adrenalin Edition.

## How to fix
1. Open **AMD Software: Adrenalin Edition > Gaming > Graphics** (global settings or per game). Menu names can differ between driver versions.
2. **Radeon Anti-Lag:** On. AMD describes it as reducing input lag in GPU-limited cases by pacing the processor's work [1].
3. **Radeon Chill:** Off, unless you want to limit frame rate for less heat and noise. Anti-Lag and Chill cannot be on at the same time [1].
4. **Radeon Boost:** Off if you notice the lower resolution during fast movement; it raises frame rate by rendering at a lower resolution while you move.
5. **Wait for Vertical Refresh:** "Off, unless application specifies", and **Enhanced Sync** off if you see stutter with FreeSync.
6. **Gaming > Display:** turn **AMD FreeSync** on for a FreeSync monitor.
7. **Record & Stream:** turn off Instant Replay if you do not use it; it records in the background.

## How to check the fix
AMD Software shows the active values per game. The Health page benchmark measures the effect.

## Sources
1. https://gpuopen.com/manuals/adlx/adlx-sdk-references/adlx-interfaces/3d-graphics/iadlx3dantilag/setenabled/
