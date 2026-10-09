# Visual effects: best performance

## Summary
Turns off window animations, shadows and similar effects, like "Adjust for best performance". Helps mainly on PCs with integrated graphics.

## How it works
The app sets the same values as Settings > System > About > Advanced system settings > Performance > "Adjust for best performance": VisualFXSetting, the UserPreferencesMask bits, minimize animation and taskbar and list view effects [1]. Sign out and in again to apply all of them.

## Why it can help
The desktop compositor has less work. On integrated graphics that shares memory with the CPU, this leaves more for games running in a window.

## Evidence
With a dedicated graphics card, the effect on games is not measurable; the desktop just feels snappier to some users.

## Trade-offs & risks
Windows looks plainer: no animations, no shadows under windows, no smooth list scrolling.

## When not to use it
Not needed with a dedicated graphics card unless you prefer the look.

## Sources
1. https://github.com/ChrisTitusTech/winutil
