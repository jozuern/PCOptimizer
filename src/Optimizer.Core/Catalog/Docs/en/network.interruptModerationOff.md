# Interrupt moderation off (Expert)

## Summary
Makes the wired network adapter report every packet right away instead of grouping them. Saves microseconds inside the PC, costs processor time. Disputed.

## How it works
With interrupt moderation the adapter waits briefly and reports several received packets with one interrupt [1]. Turned off, it raises an interrupt for every packet. The app changes the standard keyword [2] only if your adapter's driver offers it, and restarts the adapter.

## Why it can help
Each received packet reaches the game a little sooner, by the time the adapter would otherwise wait [1]. Microsoft describes this kind of delay inside the PC as usually measured in microseconds [3].

## Evidence
The time a packet travels to a game server is measured in milliseconds, and this setting does not shorten it [3]. The gain is therefore far smaller than normal ping variation. We know of no measurements that show a benefit in games.

## Trade-offs & risks
Every packet causes an interrupt, which costs processor time at high data rates such as downloads or streaming [3].

## When not to use it
If you download or stream while playing.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/interrupt-moderation
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/enumeration-keywords
3. https://learn.microsoft.com/en-us/windows-server/networking/technologies/network-subsystem/net-sub-performance-tuning-nics
