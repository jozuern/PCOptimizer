# NVIDIA shader cache: unlimited

## Summary
Lets the NVIDIA shader cache on disk grow without the driver's size limit, so compiled shaders are not deleted and recompiled.

## How it works
Games compile their shaders for your graphics card; the driver stores the results on disk and reuses them next time. When the cache reaches its size limit, the driver deletes older entries. This sets the cache size in the global profile to unlimited, like "Shader Cache Size: Unlimited" in the NVIDIA Control Panel [1].

## Why it can help
If you play many large games, a full cache means shaders are compiled again, which shows as stutter the first minutes of a session or after driver updates.

## Evidence
The benefit depends on how many games you play and how large their shader sets are; with a few games the default limit is often enough.

## Trade-offs & risks
The cache uses more disk space (several GB with many games). It is still cleared on driver updates and can be deleted with Disk Cleanup.

## When not to use it
On a small, nearly full system drive.

## Sources
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
