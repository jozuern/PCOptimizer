# Network adapter power saving off

## Summary
Turns off Energy Efficient Ethernet and selective suspend on wired network adapters. Can fix link drops on some adapters. Restarts the adapter.

## How it works
With Energy Efficient Ethernet (IEEE 802.3az), the adapter puts the link into a low power idle state between bursts of traffic, and both ends wake up when data needs to be sent [1][2]. Selective suspend lets an idle adapter power down [3]. Windows defines a standard driver setting for each of the two [1][3]. The app sets both to off, but only if your adapter's driver offers them with an off value. Power saving options that only one vendor's driver has are not changed. The adapter restarts so the driver reads the new values; the connection drops for a few seconds. Undo restores every value.

## Why it can help
Waking the link adds a small amount of latency [2]. More important in practice: some adapters lose the connection with Energy Efficient Ethernet on, and turning it off is the vendor's workaround [4].

## Evidence
The wake delay is too small to notice in games [2]. The benefit is fewer disconnects on hardware with such problems [4].

## Trade-offs & risks
Slightly higher power use. The adapter restarts once when applying and once on undo.

## When not to use it
If your connection is stable and power use matters, for example on a laptop.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/standardized-inf-keywords-for-power-management
2. https://edc.intel.com/content/www/us/en/design/products/ethernet/adapters-and-devices-user-guide/other-power-options
3. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/standardized-inf-keywords-for-ndis-selective-suspend
4. https://www.asus.com/support/faq/1052466
