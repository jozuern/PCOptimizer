# Mouse acceleration off

## Summary
Turns off "Enhance pointer precision", so the same hand movement always moves the cursor the same distance.

## How it works
With the option on, Windows applies pointer acceleration: when the mouse moves faster than two thresholds, Windows multiplies the distance [2]. The app sets MouseSpeed (the acceleration level), MouseThreshold1 and MouseThreshold2 to 0 in your user profile and applies them to the running session [1]. The setting is "Enhance pointer precision" in the Mouse Properties dialog (Settings > Bluetooth & devices > Mouse > Additional mouse settings > Pointer Options).

## Why it can help
Constant scaling makes aiming predictable in games that use the Windows cursor (many strategy games, older games, menus).

## Evidence
Games that read raw mouse input (WM_INPUT) get the movement without pointer acceleration, so this setting does not change aiming there [3]. It matters in games and menus that use the Windows cursor.

## Trade-offs & risks
The cursor may feel slower at first on the desktop; adjust the pointer speed or mouse DPI to taste.

## When not to use it
Leave it on if you prefer accelerated movement on the desktop and only play raw-input games.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
2. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mouse_event
3. https://learn.microsoft.com/en-us/windows/win32/dxtecharts/taking-advantage-of-high-dpi-mouse-movement
