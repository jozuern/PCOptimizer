# USB selective suspend off

## Summary
Stops Windows from putting idle USB devices to sleep on mains power. Can help when a mouse, headset or controller drops out briefly.

## How it works
With selective suspend, Windows suspends USB devices that report idle and wakes them on activity [1]. Some devices or hubs wake up late or not at all. The app sets the plan value for mains power to Disabled.

## Why it can help
Input devices that wake late can miss the first movement or disconnect for a moment. Without suspend they stay active.

## Evidence
There is no frame-rate effect. It is a stability fix for specific devices and does nothing on systems without such problems.

## Trade-offs & risks
Slightly higher power draw of USB devices. Battery power on laptops is not changed.

## When not to use it
Not needed if your USB devices work without dropouts.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/usb-selective-suspend
