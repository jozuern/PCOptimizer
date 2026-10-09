# Set Ethernet speed to auto negotiation

## Summary
Sets the network adapter's Speed & Duplex setting back to Auto Negotiation, so it connects at the highest speed both sides support.

## How it works
The app writes the standard driver keyword for speed and duplex (value 0 = auto negotiation) [1] in the adapter's settings and restarts the adapter, the same as changing it in Device Manager. The connection drops for a few seconds. Undo restores the previous value.

## Why it can help
A fixed speed below the adapter's maximum limits downloads and updates. With auto negotiation, adapter and router agree on the fastest speed both support.

## Evidence
Auto negotiation is the driver default; a fixed lower speed limits throughput by design.

## Trade-offs & risks
Rarely, an old switch negotiates badly and a fixed speed was set to work around it. Undo restores it.

## When not to use it
If someone fixed the speed on purpose because of a faulty switch.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/enumeration-keywords
