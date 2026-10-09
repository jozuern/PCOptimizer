# Optimizations for windowed games

## Summary
Lets older DirectX 10 and 11 games in windowed or borderless mode use the flip presentation model, which usually lowers latency.

## How it works
Older games present frames with the "blt" model, where the desktop compositor copies every frame. With this setting, Windows upgrades them to the flip model, which hands frames to the display without the extra copy [1][2]. The app sets SwapEffectUpgradeEnable=1 in your DirectX settings and keeps your other values.

## Why it can help
Windowed and borderless games skip the extra copy, which reduces frame latency [2].

## Evidence
Microsoft documents the switch from the blt model to the flip model and states that flip model generally results in lower latency, without giving numbers [1][2]. Games that already use flip, including all DirectX 12 games, do not change [1].

## Trade-offs & risks
Microsoft notes that the flip model can cause tearing at frame rates above the refresh rate; a frame limit, V-Sync or a variable refresh rate display avoids it [1]. If a single game has problems, you can exclude it in Settings > System > Display > Graphics [2].

## When not to use it
Windows forces this on while Auto HDR is on, so there is nothing to change then [2].

## Sources
1. https://devblogs.microsoft.com/directx/updates-in-graphics-and-gaming/
2. https://support.microsoft.com/en-us/topic/3f006843-2c7e-4ed0-9a5e-f9389e535952
