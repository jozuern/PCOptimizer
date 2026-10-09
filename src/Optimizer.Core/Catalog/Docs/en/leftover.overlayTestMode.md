# Remove old MPO value (OverlayTestMode)

## Summary
Removes OverlayTestMode from the DWM settings. Older guides set it to 5 to turn off MPO; on 24H2 and newer it no longer does that and is linked to black flashes.

## How it works
The value OverlayTestMode = 5 under HKLM\SOFTWARE\Microsoft\Windows\Dwm used to disable multiplane overlay. Users report that it stopped working with 24H2 and can cause black flashes [1]. Deleting it restores Windows' default. Takes effect after a restart.

## Why it can help
Removes a value that no longer has its intended effect but can cause display glitches.

## Evidence
Based on community reports for 24H2 and newer; there is no Microsoft documentation of the value.

## Trade-offs & risks
If you still need MPO off, use the tweak "Multiplane overlay (MPO) off" instead.

## When not to use it
Nothing to consider if the value exists; if it does not, this item is already clean.

## Sources
1. https://forums.guru3d.com/goto/post?id=6305178
