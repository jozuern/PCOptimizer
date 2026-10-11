# Game Bar recording off by policy

## Summary
Turns off Windows game recording and broadcasting with the documented policy, so background recording stops. Pro, Enterprise and Education.

## How it works
The policy AllowGameDVR = 0 under SOFTWARE\Policies\Microsoft\Windows\GameDVR turns off the Windows Game Recording and Broadcasting features [1]. Microsoft lists it for Pro, Enterprise and Education [1]. It is the documented way to do what "Game Bar captures off" does with the values behind the Settings switches.

## Why it can help
With background recording on, Game Bar keeps recording the last minutes of a game, which uses the video encoder of the graphics card and the drive. With the policy, Game Bar records nothing.

## Evidence
Documented policy [1]. An effect on games is only possible while background recording was on, and then situational.

## Trade-offs & risks
Game Bar can no longer record clips or broadcast. Recording with other programs such as NVIDIA App or OBS still works.

## When not to use it
If you record clips or stream with Game Bar.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement
