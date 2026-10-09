# Set the display to its highest refresh rate

## Summary
Sets the monitor to the highest refresh rate Windows offers at its current resolution, the same as choosing it in Settings > Display.

## How it works
The app changes the display mode with the Windows display API: same resolution, highest offered refresh rate [1]. The change is saved for the display. Undo sets the previous rate again.

## Why it can help
At 144 Hz instead of 60 Hz the screen shows a new image every 6.9 ms instead of every 16.7 ms: smoother motion and less delay.

## Evidence
The refresh rate directly limits how many frames you can see; the gain is large whenever the display was below its maximum.

## Trade-offs & risks
If the screen stays black or flickers, wait 15 seconds: Windows keeps the previous mode if the new one is not confirmed. A cable that cannot carry the rate is the usual cause.

## When not to use it
Not needed if the display already runs at its maximum.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-changedisplaysettingsexw
