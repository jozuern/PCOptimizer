# Run games on the dedicated graphics card

## Summary
Sets "High performance" as Windows graphics preference for each detected game, so Windows asks for the dedicated GPU instead of the integrated one.

## How it works
Windows stores a per-app GPU preference in your user profile (UserGpuPreferences, the same list as Settings > System > Display > Graphics). The app adds the entry GpuPreference=2 for each detected game executable and keeps other values. The value 2 matches the DirectX preference "high performance", which asks for the highest performing GPU, such as a discrete graphics card [1]. Microsoft documents the DirectX preference but not this registry list itself; the entries are the ones the Settings page writes.

## Why it can help
On PCs with two GPUs, a game that ends up on the integrated GPU runs at a much lower frame rate. Without a preference, DirectX lists the adapter that drives the main display first [2], which on laptops with hybrid graphics is usually the integrated GPU.

## Evidence
The graphics driver can already route games to the graphics card: NVIDIA's driver profiles tell the system which games need the GeForce GPU [3]. The Windows preference helps when a game is not covered by such a profile. Whether a game was affected before cannot be told without checking which GPU it used (Task Manager > Performance > GPU).

## Trade-offs & risks
Slightly higher power draw on laptops while those games run. If the guessed executable is wrong (for example a launcher), add the right one in Settings > System > Display > Graphics.

## When not to use it
Not needed on PCs with only one GPU, or when Task Manager already shows the game on the graphics card.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_6/ne-dxgi1_6-dxgi_gpu_preference
2. https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgifactory-enumadapters
3. https://www.nvidia.com/en-us/geforce/news/rtx-laptops-advanced-optimus/
