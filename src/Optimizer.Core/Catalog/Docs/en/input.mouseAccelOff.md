# Mouse acceleration off

## Summary
Turns off "Enhance pointer precision", so the same hand movement always moves the cursor the same distance.

## How it works
With the option on, Windows scales pointer movement with a speed-dependent curve: a fast flick moves the cursor further than a slow movement over the same physical distance. The app sets MouseSpeed, MouseThreshold1 and MouseThreshold2 to 0 in your user profile and applies them to the running session [1].

## Why it can help
Constant scaling makes aiming predictable in games that use the Windows cursor (many strategy games, older games, menus).

## Evidence
Games that read raw mouse input (most current competitive shooters) bypass this setting, so they are not affected either way.

## Trade-offs & risks
The cursor may feel slower at first on the desktop; adjust the pointer speed or mouse DPI to taste.

## When not to use it
Leave it on if you prefer accelerated movement on the desktop and only play raw-input games.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
