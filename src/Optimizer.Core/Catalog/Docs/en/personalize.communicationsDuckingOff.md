# Game audio stays loud during voice calls

## Summary
Windows no longer lowers other sounds, such as your game, by 80 percent when it detects a call or voice chat.

## How it works
On the Communications tab of the Sound control panel you choose what happens during communication: lower other sounds (80 percent by default), mute them, or do nothing [1]. The app chooses "Do nothing" by setting UserDuckingPreference to 3. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
Game audio does not drop when Discord or another voice app opens the microphone.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
Calls can be harder to hear over loud audio; you set the levels yourself.

## When not to use it
If you like Windows to turn other sounds down during calls.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/coreaudio/stream-attenuation
