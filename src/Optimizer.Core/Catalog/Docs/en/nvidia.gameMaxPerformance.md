# NVIDIA: prefer maximum performance for this game

## Summary
Sets "Prefer maximum performance" only in this game's NVIDIA profile. Keeps clocks up while the game runs, everything else stays as it is.

## How it works
NVIDIA stores settings per game in driver profiles [1]. The app writes the power management mode into the profile this game belongs to. If the driver has no profile for the game, the app creates one named "PCOptimizer: " plus the game's file name. Undo removes the setting again.

## Why it can help
In light or CPU-bound games the graphics card can drop to lower clocks between frames, which can cause uneven frame times.

## Evidence
The effect is situational; in demanding games the card already runs at full clocks.

## Trade-offs & risks
More power use and heat while the game runs. An empty profile created by the app stays after undo; it has no effect.

## When not to use it
On laptops on battery, or in games that already run at full GPU load.

## Sources
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
