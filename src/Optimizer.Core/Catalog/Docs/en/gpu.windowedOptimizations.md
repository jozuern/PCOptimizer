# Optimizations for windowed games

## Summary
Lets older DirectX 10/11 games in windowed or borderless mode use the faster flip presentation model. Lowers latency in borderless games.

## How it works
Older games present frames with the "blt" model, where the desktop compositor copies every frame. With this setting, Windows upgrades them to the flip model, which hands frames to the display without the extra copy [1]. The app sets SwapEffectUpgradeEnable=1 in your DirectX settings and keeps your other values.

## Why it can help
Borderless games get latency close to exclusive fullscreen, and features like Auto HDR and variable refresh rate work in the window.

## Evidence
Microsoft documents the change of presentation model; the latency gain is measurable in borderless DX10/11 games and zero in games that already use flip [2].

## Trade-offs & risks
Rare incompatibilities with very old games or capture tools; individual games can be excluded in Settings > Display > Graphics.

## When not to use it
Windows forces this on while Auto HDR is on, so there is nothing to change then.

## Sources
1. https://devblogs.microsoft.com/directx/optimizations-for-windowed-games-in-windows-11/
2. https://support.microsoft.com/en-us/topic/3f006843-2c7e-4ed0-9a5e-f9389e535952
