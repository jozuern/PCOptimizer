# Nagle's algorithm off (TCP)

## Summary
Disables delayed ACKs and Nagle's packet bundling on your active network adapters. Only affects TCP; most game traffic uses UDP.

## How it works
Nagle's algorithm collects small TCP packets into larger ones before sending [1]. TcpAckFrequency = 1 and TCPNoDelay = 1 under each active interface make Windows send acknowledgements and small packets immediately.

## Why it can help
Older games that send small, frequent TCP packets may see lower delays.

## Evidence
Games that care already set TCP_NODELAY on their own sockets, and most real-time game traffic uses UDP, which Nagle does not touch. Measurements rarely show a difference.

## Trade-offs & risks
Slightly more packets on the network for bulk transfers.

## When not to use it
Not needed for current games. Values are set per interface; a new adapter does not get them.

## Sources
1. https://www.rfc-editor.org/rfc/rfc896
