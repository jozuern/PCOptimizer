# Fullscreen optimizations off (global)

## Summary
Makes exclusive-fullscreen games run in classic exclusive mode instead of Windows' optimized borderless mode. Often recommended, but effects are inconsistent.

## How it works
Since Windows 10, games requesting exclusive fullscreen often run in an optimized borderless mode that behaves like exclusive fullscreen but allows overlays and fast Alt-Tab [1]. These values tell Windows to honor real exclusive fullscreen for all games.

## Why it can help
Some older games show lower latency or fewer stutters in true exclusive mode.

## Evidence
Microsoft states that the optimized mode matches exclusive fullscreen performance [1]. User reports differ per game. Treat it as disputed.

## Trade-offs & risks
Slower Alt-Tab and possible problems with overlays and HDR. Per game, the same option exists in the exe's Properties > Compatibility.

## When not to use it
Prefer the per-game setting for a game that misbehaves instead of the global switch.

## Sources
1. https://devblogs.microsoft.com/directx/demystifying-full-screen-optimizations/
