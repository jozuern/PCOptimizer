# Interrupt moderation off (Expert)

## Summary
Makes the wired network adapter signal every packet right away instead of grouping them. Can lower delay by microseconds, costs processor time. Disputed.

## How it works
With interrupt moderation the adapter waits briefly and reports several received packets with one interrupt [1]. Turned off, it raises an interrupt for every packet. The app changes the standard keyword only if your adapter's driver offers it, and restarts the adapter.

## Why it can help
Each packet reaches the game a little sooner, by the moderation delay (typically microseconds to a fraction of a millisecond, depending on the driver).

## Evidence
Network latency to a game server is measured in milliseconds, so the gain is usually lost in normal variation. Measurements on gaming PCs show no consistent improvement.

## Trade-offs & risks
More interrupts mean more processor load at high data rates (downloads, streaming), which can cost frame rate on processors with few cores.

## When not to use it
On processors with few cores, or if you download while playing.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/interrupt-moderation
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/enumeration-keywords
