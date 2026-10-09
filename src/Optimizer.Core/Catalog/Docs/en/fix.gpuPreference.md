# Run games on the dedicated graphics card

## Summary
Sets "High performance" as graphics preference for each detected game, so Windows runs it on the dedicated GPU instead of the integrated one.

## How it works
Windows stores a per-app GPU preference in your user profile (UserGpuPreferences, the same list as Settings > Display > Graphics). GpuPreference=2 means high performance [1]. The app adds an entry for each detected game executable and keeps other values.

## Why it can help
On PCs with two GPUs, a game left on the integrated GPU runs at a fraction of the frame rate.

## Evidence
The difference between integrated and dedicated graphics is large in every 3D game.

## Trade-offs & risks
Slightly higher power draw on laptops while those games run. If the guessed executable is wrong (for example a launcher), add the right one in Settings > Display > Graphics.

## When not to use it
Not needed on PCs with only one GPU.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_6/nf-dxgi1_6-idxgifactory6-enumadapterbygpupreference
