# NVIDIA texture filtering: high performance

## Summary
Sets texture filtering quality to "High performance". NVIDIA gives no numbers for the frame rate gain, and we found no measurement. Disputed.

## How it works
The setting "Texture filtering - Quality" controls driver optimizations for texture sampling; "Quality" is the default for GeForce products [1]. "High performance" allows the most aggressive optimizations [1]. The app writes the value from NVIDIA's settings header into the global profile [2].

## Why it can help
On older or very weak graphics cards, cheaper texture filtering can save a little GPU time.

## Evidence
NVIDIA describes High performance as the option with the highest frame rate, without numbers [1]. We found no published measurement of the gain on current graphics cards. That is why the impact is rated 0.

## Trade-offs & risks
Textures can look worse, for example less sharp at a distance.

## When not to use it
On any current graphics card where image quality matters more than an unmeasured gain.

## Sources
1. https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_(reference).htm
2. https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h
