# Variable refresh rate for older full screen games

## Summary
Lets a G-SYNC, FreeSync or Adaptive-Sync monitor follow the frame rate in DirectX 11 full screen games that do not support it themselves. Off by default.

## How it works
Variable refresh rate (VRR) lets a capable monitor, including AMD FreeSync, NVIDIA G-SYNC and VESA DisplayPort Adaptive-Sync, adjust its refresh rate to the frame rate [1]. The Windows setting "Variable refresh rate" enables it for DirectX 11 full screen games that do not support VRR natively [1]. It is off by default and appears only with the necessary drivers and a VRR capable monitor [2]. The app adds VRROptimizeEnable=1 to DirectXUserGlobalSettings and keeps the other entries in that value. Microsoft does not document the registry value behind the switch; the tutorial [2] shows the value the switch writes. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
When the frame rate drops below the refresh rate, the monitor waits for the next frame instead of showing a torn or repeated one.

## Evidence
Microsoft describes what the setting does [1]; a measured gain depends on the game and the monitor, so the impact is situational. The VRR mode of the monitor and the graphics driver must be on as well.

## Trade-offs & risks
Without a VRR capable monitor and driver the setting changes nothing [2]. Games that already support VRR are not affected [1].

## When not to use it
If your monitor has no VRR or you play only games that support VRR themselves.

## Sources
1. https://devblogs.microsoft.com/directx/navigating-the-redesigned-graphics-settings-page/
2. https://www.elevenforum.com/t/enable-or-disable-variable-refresh-rate-for-games-in-windows-11.12052/
