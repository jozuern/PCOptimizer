# Xbox button does not open Game Bar

## Summary
The Xbox button of a controller no longer opens Game Bar, so pressing it in a game does not bring up the overlay. Value Microsoft does not document, tested.

## How it works
Game Bar has a setting that lets the Xbox button of a controller open it. The app sets UseNexusForGameBarEnabled = 0, the value Win11Debloat writes for that setting [1]. It is the value behind "Allow your controller to open Game Bar" in Settings > Gaming > Game Bar. Microsoft does not document the value.

## Why it can help
Pressing the Xbox button by accident no longer interrupts a game, and the button stays free for launchers such as Steam Big Picture.

## Evidence
In a VM test on Windows 11 build 26300, Settings showed "Allow your controller to open Game Bar" Off after apply and On after undo. No effect on frame rate or latency.

## Trade-offs & risks
Windows + G still opens Game Bar.

## When not to use it
If you open Game Bar with the controller.

## Sources
1. https://github.com/Raphire/Win11Debloat/blob/master/Regfiles/Disable_Game_Bar_Integration.reg
