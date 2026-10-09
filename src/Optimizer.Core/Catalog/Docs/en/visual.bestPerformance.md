# Visual effects: best performance

## Summary
Turns off animations, fades and shadows, similar to "Adjust for best performance" but keeps font smoothing and thumbnails. Mainly useful on PCs with integrated graphics.

## How it works
The app sets most of the options behind Adjust the appearance and performance of Windows > Visual Effects [1]: menu, list, tooltip, minimize and taskbar animations, fades, cursor and window shadows, Peek and dragging with window contents. Several of these options are bits of the UserPreferencesMask value [2]. Smooth edges of screen fonts and thumbnails stay on, so the dialog shows "Custom". Sign out and in again to apply all of them.

## Why it can help
Microsoft says visual effects use system resources and turning them off can improve responsiveness [1]. On integrated graphics that shares memory with the CPU, this may leave a little more for games in a window.

## Evidence
There is no published measurement of a frame rate gain. With a dedicated graphics card no effect on games is expected; the desktop may feel quicker.

## Trade-offs & risks
Windows looks plainer: no animations, no shadows under windows and menus, no smooth list scrolling.

## When not to use it
Not needed with a dedicated graphics card unless you prefer the look.

## Sources
1. https://support.microsoft.com/en-us/windows/tips-to-improve-pc-performance-in-windows-b3b3ef5b-5953-fb6a-2528-4bbed82fba96
2. https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-2000-server/cc957204(v=technet.10)
