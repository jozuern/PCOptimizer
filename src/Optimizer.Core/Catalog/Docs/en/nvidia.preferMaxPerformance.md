# NVIDIA power mode: prefer maximum performance (global)

## Summary
Keeps the graphics card at high clocks while any 3D application runs. Can steady frame times in light games, raises power use. Per game is usually better.

## How it works
By default the driver lowers the graphics clock when the load is low. "Prefer maximum performance" in the global profile [1] tells it to stay at the highest performance level while a 3D application runs.

## Why it can help
In light or older games the card can drop to lower clocks between frames and needs time to clock up again, which can cause uneven frame times.

## Evidence
The effect is situational; in demanding games the card already runs at full clocks and nothing changes.

## Trade-offs & risks
Higher power draw, heat and fan noise, also with browsers or launchers that use 3D acceleration. With several monitors the card may stay at high clocks at the desktop.

## When not to use it
Prefer the per-game version on the graphics page, which only affects the games you choose.

## Sources
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
