# Auto HDR on

## Summary
Older DirectX 11 and 12 games that only output SDR are shown with HDR on an HDR display. HDR itself must be on.

## How it works
Auto HDR raises color range and brightness of SDR games that use DirectX 11 or 12 on an HDR-capable display; it is under Settings > System > Display > HDR [1]. The app sets the AutoHDREnable token in DirectXUserGlobalSettings, next to the windowed game optimizations. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
Older games look closer to HDR games on an HDR monitor.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
The result depends on the game; some look washed out or too bright. Without an HDR display or with HDR off, nothing changes.

## When not to use it
If you have no HDR display, or prefer games in their original look.

## Sources
1. https://support.microsoft.com/en-us/windows/hardware/display-graphics/use-auto-hdr-for-better-gaming-in-windows
