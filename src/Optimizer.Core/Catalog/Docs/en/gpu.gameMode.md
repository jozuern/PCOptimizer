# Game Mode on

## Summary
Makes sure Windows Game Mode is on. It gives the running game priority and blocks driver installs and restart prompts from Windows Update while you play.

## How it works
When Game Mode detects a game, Windows prioritizes its threads, limits background work and holds back Windows Update driver installs and restart notifications [1]. It is on by default; the app sets it back on if it was turned off.

## Why it can help
Fewer interruptions from background tasks mean fewer stutters, especially on CPUs with few cores.

## Evidence
Average frame rates change little. The benefit shows in fewer interruptions rather than higher FPS.

## Trade-offs & risks
Rarely, a game behaves worse with Game Mode; it can be turned off again.

## When not to use it
Keep it on. On Ryzen X3D processors with two chiplets, Game Mode is part of how the AMD driver detects games.

## Sources
1. https://www.elevenforum.com/t/turn-on-or-off-game-mode-in-windows-11.1447/
