# Network throttling off (NetworkThrottlingIndex)

## Summary
Removes the limit MMCSS places on network packet processing while multimedia plays. Often recommended for games; effect disputed.

## How it works
While multimedia threads run, MMCSS limits non-multimedia network processing to protect audio and video playback [1]. NetworkThrottlingIndex = 0xFFFFFFFF disables that limit.

## Why it can help
If a game streams music or voice through MMCSS-registered threads, network packets are no longer held back.

## Evidence
The limit applies to roughly 10 packets per millisecond, far more than any game sends. Measurements show no difference for game traffic.

## Trade-offs & risks
Audio could glitch under extreme network load on old hardware.

## When not to use it
Not needed; keep it only if a specific game benefits for you.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
