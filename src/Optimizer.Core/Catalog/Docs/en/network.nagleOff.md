# Delayed TCP acknowledgements off (TcpAckFrequency)

## Summary
Makes Windows acknowledge every received TCP segment right away instead of waiting. Affects only TCP connections. Effect on games disputed.

## How it works
By default Windows acknowledges every second TCP segment, or a single segment once a 200 ms timer runs out, to send fewer packets [1]. TcpAckFrequency = 1 under each active network interface makes Windows acknowledge every segment immediately [1]. Nagle's algorithm is a different mechanism on the sending side that holds back small packets while earlier data is unacknowledged [2]. Programs switch it off for their own connections with the TCP_NODELAY option [3]. Microsoft documents no registry value that turns Nagle off for the whole system, so the app does not set one.

## Why it can help
If a game server sends small TCP messages and waits for acknowledgements before sending more, faster acknowledgements can shorten that wait.

## Evidence
Microsoft does not recommend changing the default without careful study of the environment [1]. Whether a game uses TCP or UDP for its real-time traffic depends on the game; UDP has no acknowledgements, so this setting does not affect it. We know of no measurements that show a gain in current games.

## Trade-offs & risks
More acknowledgement packets on the network, especially during large downloads.

## When not to use it
Not needed unless a specific TCP-based game shows a measurable benefit. The value is set per interface; an adapter connected later does not get it.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/registry-entry-control-tcp-acknowledgment-behavior
2. https://www.rfc-editor.org/rfc/rfc896
3. https://learn.microsoft.com/en-us/windows/win32/winsock/ipproto-tcp-socket-options
