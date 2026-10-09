# NVIDIA texture filtering: high performance

## Summary
Sets texture filtering quality to "High performance". The frame rate gain is usually below measurement noise. Disputed.

## How it works
The setting "Texture filtering - Quality" controls driver optimizations for texture sampling. "High performance" allows the most aggressive optimizations [1]. It is stored in the global profile.

## Why it can help
On older or very weak graphics cards, cheaper texture filtering can save a little GPU time.

## Evidence
On current graphics cards texture filtering costs very little, so tests rarely show a measurable difference. That is why the impact is rated 0.

## Trade-offs & risks
Textures can shimmer or look less sharp at a distance.

## When not to use it
On any current graphics card where image quality matters more than an unmeasurable gain.

## Sources
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
