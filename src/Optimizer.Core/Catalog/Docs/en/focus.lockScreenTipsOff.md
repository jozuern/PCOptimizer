# Lock screen: no fun facts and tips

## Summary
Turns off "Get fun facts, tips, tricks, and more on your lock screen". Applies when the lock screen shows a picture or slideshow, not Windows Spotlight.

## How it works
With a picture or slideshow as the lock screen background, the option "Get fun facts, tips, tricks, and more on your lock screen" adds content from Microsoft [1]. The option is offered only for a picture or slideshow [2]. The app sets RotatingLockScreenOverlayEnabled and SubscribedContent-338387Enabled to 0. Microsoft does not document the registry value behind the switch; the tutorial [2] shows the value the switch writes. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
A quieter lock screen without Microsoft content.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
None beyond the missing tips. Windows Spotlight keeps showing its own content [1].

## When not to use it
If you like the tips on the lock screen.

## Sources
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-lock-screen-in-windows
2. https://www.elevenforum.com/t/enable-or-disable-facts-tips-and-tricks-on-lock-screen-in-windows-11.7079/
