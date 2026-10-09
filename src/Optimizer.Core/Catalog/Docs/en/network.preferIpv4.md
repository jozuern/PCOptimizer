# Prefer IPv4 over IPv6

## Summary
Makes Windows try IPv4 before IPv6 when a server offers both. IPv6 stays enabled. Microsoft recommends this instead of turning IPv6 off.

## How it works
By default Windows prefers IPv6 addresses over IPv4 addresses [1]. Bit 0x20 of the registry value DisabledComponents changes the default prefix policy so IPv4 is used first [1]. The app only sets this bit and keeps the other bits. Takes effect after a restart [1].

## Why it can help
Helps with networks or game servers where IPv6 routes are slower or unreliable.

## Evidence
No effect when IPv6 works well. Microsoft recommends this instead of disabling IPv6, which can break Windows features [1].

## Trade-offs & risks
Servers that only have an IPv6 address are still reached over IPv6; only the order changes.

## When not to use it
Not needed if your connection has no IPv6 problems.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/configure-ipv6-in-windows
