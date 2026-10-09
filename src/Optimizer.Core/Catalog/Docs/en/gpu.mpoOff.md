# Multiplane overlay (MPO) off

## Summary
Troubleshooting only: turns off multiplane overlay to fix flicker, black screens or frozen windows in some apps. No performance gain.

## How it works
Multiplane overlay lets the display engine combine several layers (for example a video and the desktop) in hardware instead of in the compositor. On some driver and monitor combinations it causes flicker. The value DisableOverlays = 1 under GraphicsDrivers is reported by users to turn it off on 24H2 and newer [1].

## Why it can help
Removes the cause of MPO-related flicker and freezes when they occur.

## Evidence
There is no Microsoft documentation for this value; it is based on community reports [1]. dxdiag does not reliably show whether MPO is in use [2]. Without MPO problems there is nothing to gain.

## Trade-offs & risks
Can increase power draw during video playback, because the compositor does more work. Needs a restart.

## When not to use it
Only use it if you see flicker or black flashes. The old OverlayTestMode = 5 value no longer works on 24H2 and is reported by the leftover check instead.

## Sources
1. https://www.guru3d.com/publish/comments/geforce-56603-whql-driver-download/page-17
2. https://www.techpowerup.com/forums/goto/post?id=5552960
