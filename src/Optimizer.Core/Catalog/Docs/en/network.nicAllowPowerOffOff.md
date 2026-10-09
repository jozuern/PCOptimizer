# Do not let Windows turn off the network adapter

## Summary
Clears "Allow the computer to turn off this device to save power" for wired adapters. Only changes what happens when the PC goes to sleep; also stops wake from sleep.

## How it works
The Power Management tab of the adapter in Device Manager is stored as the PnPCapabilities value. According to Microsoft, the option only controls how the adapter is handled when the PC goes to sleep: when it is on, Windows puts the adapter into a low power state and back; when it is off, Windows halts the adapter and starts it fresh on resume. Windows never turns the adapter off because it is idle [1]. A value of 24 clears the option and also stops the adapter from waking the PC [1]. The app writes it for physical Ethernet adapters and restarts them. Undo restores the previous state.

## Why it can help
Some drivers claim to support sleep states but do not come back correctly after the PC resumes from sleep. Starting the adapter fresh avoids that [1].

## Evidence
Microsoft documented this value for drivers that misreport how they handle sleep, in a support article it has since retired; the source link points to the last published version in Microsoft's documentation repository [1]. The article was written for Windows 7 and does not apply to newer NetAdapterCx drivers [1]. There is no effect while you play.

## Trade-offs & risks
Wake-on-LAN and waking the PC through the network stop working.

## When not to use it
If your connection works after sleep, or if you wake the PC over the network.

## Sources
1. https://github.com/MicrosoftDocs/SupportArticles-docs/blob/63017aecf99ce46fc89302444a1b2f4537698ae4/support/windows-client/networking/power-management-on-network-adapter.md
