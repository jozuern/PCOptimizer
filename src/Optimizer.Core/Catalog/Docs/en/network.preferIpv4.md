# Prefer IPv4 over IPv6

## Summary
Makes Windows try IPv4 before IPv6 when a server offers both. IPv6 stays enabled.

## How it works
The registry value DisabledComponents controls IPv6 behavior; bit 0x20 changes the address preference so IPv4 is used first [1]. The app only sets this bit and keeps other bits. Takes effect after a restart.

## Why it can help
Helps with networks or game servers where IPv6 routes are slower or unreliable.

## Evidence
No effect when IPv6 works well. Microsoft recommends this instead of disabling IPv6, which can break Windows features [1].

## Trade-offs & risks
Services that only reach you over IPv6 still work; only the order changes.

## When not to use it
Not needed if your connection has no IPv6 problems.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/configure-ipv6-in-windows
