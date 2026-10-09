# NVIDIA Low Latency Mode: On

## Summary
Limits the frames the processor prepares ahead of the graphics card to one. Lowers input delay in GPU-bound DirectX 9 and 11 games.

## How it works
Normally the driver lets the processor queue a few frames ahead so the graphics card never waits. "Low Latency Mode: On" in the NVIDIA Control Panel limits the queue to one frame, the same as the former setting "Maximum pre-rendered frames" = 1 [1][2]. The app writes this value into the global profile through NVIDIA's driver settings API; the setting is listed in NVIDIA's public settings header [4]. DirectX 12 and Vulkan games decide their queue themselves and are not affected [1].

## Why it can help
When the graphics card is the bottleneck, every queued frame adds one frame of delay between your input and the image. With one frame instead of several, the delay drops by up to the length of the removed frames.

## Evidence
NVIDIA says the low latency modes have the most effect when a game is GPU-bound at about 60 to 100 FPS, and that DirectX 12 and Vulkan games decide their queue themselves [1]. For games with NVIDIA Reflex, NVIDIA recommends Reflex over the driver's low latency setting [3].

## Trade-offs & risks
In CPU-bound games a shorter queue can lower the frame rate slightly or cause uneven frame times. The "Ultra" level is not offered here.

## When not to use it
In games with NVIDIA Reflex (turn on Reflex there instead [3]), or if frame times become uneven.

## Sources
1. https://www.nvidia.com/en-us/geforce/news/gamescom-2019-game-ready-driver/
2. https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_(reference).htm
3. https://www.nvidia.com/en-us/geforce/news/reflex-low-latency-platform/
4. https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h
