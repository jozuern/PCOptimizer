# Wi-Fi power saving on battery: maximum

## Summary
Sets the power saving mode of the Wi-Fi adapter on battery from Medium (the Windows default for the Balanced plan) to Maximum. Plugged in, nothing changes.

## How it works
The power plan has a setting "Wireless adapter settings > Power saving mode" with four levels, from maximum performance (0) to maximum power saving (3) [1]. The app sets the battery value to 3. Windows passes the level to the Wi-Fi driver, which decides how long the radio may sleep between transmissions.

## Why it can help
A Wi-Fi radio that sleeps longer between transmissions uses less energy, which helps most when the laptop is mostly idle on Wi-Fi.

## Evidence
The effect depends on the Wi-Fi driver: some drivers follow the level closely, others ignore it. We could not find measurements that hold across adapters, so the impact is rated low.

## Trade-offs & risks
On battery, latency can rise and transfers can get slower, noticeable in video calls and online games. Some access points do not handle 802.11 power saving well, which can cause connection problems [3]. Undo restores the previous battery value.

## When not to use it
If you make video calls or play online on battery. If the setting is missing on your laptop, the driver does not offer it and the tweak shows as not supported.

## Sources
1. https://learn.microsoft.com/en-us/archive/blogs/richardsmith/powercfg-useful-if-you-know-the-guids
2. https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options
3. https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-7/dd744398(v=ws.10)
