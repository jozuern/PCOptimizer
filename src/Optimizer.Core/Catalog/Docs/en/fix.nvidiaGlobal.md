# Reset limiting NVIDIA global settings

## Summary
Removes the global profile's own value for the NVIDIA settings that limit games (frame rate limit, power mode), so the driver defaults apply.

## How it works
NVIDIA stores driver settings in profiles: a global profile and one per game. The app removes the listed settings from the global profile through NVIDIA's driver settings API [1]. Game profiles stay as they are. Undo writes the previous values back.

## Why it can help
A global frame rate limit far below the refresh rate, or the power mode forced to minimum, slows down every game. Without them, each game runs as the driver intends.

## Evidence
A frame rate limit caps the frame rate by design, and the minimum power mode keeps the graphics card at lower clocks; removing them restores the normal behavior.

## Trade-offs & risks
If you set the limit on purpose, for example to reduce heat or noise, you lose that. Set it per game in that case.

## When not to use it
When you deliberately use a global frame limit.

## Sources
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
