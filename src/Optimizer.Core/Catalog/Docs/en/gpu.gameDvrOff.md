# Game Bar captures off

## Summary
Turns off Xbox Game Bar captures, including "Record what happened", which keeps recording the last minutes of gameplay in the background.

## How it works
With background recording on, Windows continuously encodes the last minutes of gameplay into a buffer using the GPU's video encoder. The app turns off Game DVR, app capture and background recording for your user account, with the same values WinUtil uses [2]. Microsoft does not document these per-user values; independent tutorials show that they turn Game Bar recording [3][4] and "Record what happened" [5] off, and Microsoft's capture API uses the same setting names [6]. The documented policy "AllowGameDVR" turns off recording for the whole PC [1]; on Pro, Enterprise and Education "Game Bar recording off by policy" uses it. The Game Bar itself stays installed.

## Why it can help
Continuous recording uses GPU encoder time, memory and disk writes. Turning it off removes that load while gaming.

## Evidence
When background recording is off, which is the usual setting, this changes nothing measurable. With it on, the GPU's video encoder works all the time during play; we found no published measurement of the cost.

## Trade-offs & risks
You lose Game Bar clips and the "Record that" shortcut. Recording with NVIDIA ShadowPlay or OBS is not affected.

## When not to use it
Keep it if you use Game Bar clips.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement
2. https://github.com/ChrisTitusTech/winutil
3. https://www.tenforums.com/tutorials/8637-game-bar-turn-off-windows-10-a.html
4. https://www.thewindowsclub.com/enable-disable-game-dvr-windows-10
5. https://www.elevenforum.com/t/enable-or-disable-record-what-happened-for-gaming-captures-in-windows-11.17900/
6. https://learn.microsoft.com/en-us/uwp/api/windows.media.capture.appcapturesettings
