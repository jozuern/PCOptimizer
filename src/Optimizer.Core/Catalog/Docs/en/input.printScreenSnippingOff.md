# Print Screen copies the screen again

## Summary
The Print Screen key copies the whole screen to the clipboard instead of opening Snipping Tool, as before Windows 11 build 22621.1928.

## How it works
Since Windows 11 build 22621.1928 the Print Screen key opens Snipping Tool by default [1]. With the switch "Use the Print screen key to open screen capture" off, the key copies the screen to the clipboard [1]. The app sets PrintScreenKeyForSnippingEnabled to 0. Depending on other apps, the change can need a sign-out or restart [1]. Microsoft does not document the registry value behind the switch; the tutorial [1] shows the value the switch writes. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
One press copies the screen without a selection step, for example to paste it straight into a chat.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
To cut out a part of the screen, use Windows + Shift + S.

## When not to use it
If you use Print Screen to pick a screen area.

## Sources
1. https://www.elevenforum.com/t/enable-or-disable-use-print-screen-key-to-open-screen-snipping-in-windows-11.520/
