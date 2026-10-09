# NVIDIA Low Latency Mode: On

## Summary
Limits the number of frames the processor prepares ahead of the graphics card to one. Lowers input delay in GPU-bound DirectX 11 games.

## How it works
Normally the driver lets the processor queue a few frames ahead so the graphics card never waits. "Low Latency Mode: On" in the NVIDIA Control Panel stores a limit of one queued frame (the setting formerly called "Maximum pre-rendered frames") in the global profile [1]. DirectX 12 and Vulkan games manage their own queue and are not affected.

## Why it can help
When the graphics card is the bottleneck, every queued frame adds one frame of delay between your input and the image. With one frame instead of several, the delay drops by up to the length of the removed frames.

## Evidence
The effect follows from the queue length; it is only noticeable in GPU-bound situations. Games with NVIDIA Reflex use their own, stronger method and override this setting.

## Trade-offs & risks
In CPU-bound games a shorter queue can lower the frame rate slightly or cause uneven frame times. The "Ultra" level uses an undocumented setting and is not offered here.

## When not to use it
In games with NVIDIA Reflex (turn on Reflex there instead), or if frame times become uneven.

## Sources
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
