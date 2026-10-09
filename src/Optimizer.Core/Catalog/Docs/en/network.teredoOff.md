# Teredo tunneling off

## Summary
Disables the Teredo IPv6 tunnel. Blocked when Xbox or Game Pass is used, because Xbox networking needs Teredo.

## How it works
Teredo tunnels IPv6 traffic over IPv4 when there is no native IPv6. Bit 0x08 of DisabledComponents turns it off [1]. The app only sets this bit. Takes effect after a restart.

## Why it can help
Removes a tunnel adapter that some games and VPNs handle badly.

## Evidence
No frame-rate effect; it only matters when Teredo causes connection problems.

## Trade-offs & risks
Xbox party chat and some Xbox multiplayer features stop working without Teredo.

## When not to use it
Do not use if you play Xbox or Game Pass titles online.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/configure-ipv6-in-windows
