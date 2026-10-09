# Do not let Windows turn off the network adapter

## Summary
Clears "Allow the computer to turn off this device to save power" for wired network adapters. Avoids lost connections after idle periods.

## How it works
The adapter's Power Management tab in Device Manager is stored as the PnPCapabilities value. A value of 24 clears the option so Windows does not power the adapter down [1]. The app writes it for physical Ethernet adapters and restarts them. Undo restores the previous state.

## Why it can help
Some adapters do not come back reliably after Windows turned them off, which shows as a lost connection after the PC was idle.

## Evidence
Microsoft documents this value for adapters that lose connectivity because of power management [1]. Without such a problem there is no performance change.

## Trade-offs & risks
Slightly higher idle power. Wake-on-LAN can stop working on some adapters.

## When not to use it
On laptops, or if you use Wake-on-LAN.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/networking/power-management-on-network-adapter
