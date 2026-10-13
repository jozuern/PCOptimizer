# Windows apps: no screenshots of other windows

## Summary
Windows apps (Store and other packaged apps) can no longer capture other windows or the screen on their own, nor hide the capture border. Desktop programs are not affected. Pro and up.

## How it works
Windows lets apps capture the image of other windows or whole displays, and lets an app turn off the border that shows a capture is running. Two policies control this for Windows apps: LetAppsAccessGraphicsCaptureProgrammatic decides whether Windows apps may take screenshots of various windows or displays, and LetAppsAccessGraphicsCaptureWithoutBorder whether they may turn off the screenshot border [1]. The app sets both to Force Deny (2), so Windows apps cannot do either and the matching switches under Settings > Privacy & security are locked [1]. An app that is open when the policy changes notices it only after it restarts [1]. Desktop programs, such as most games, launchers, OBS and browsers, are not covered by these policies.

## Why it can help
A Windows app you installed cannot record what other windows show without you starting it, and a capture can no longer run without its border.

## Evidence
Two documented Windows policies [1]; Microsoft lists them for Windows 11 Pro, Enterprise and Education [1]. A privacy setting without effect on frame rate or latency. It stays a preview until a test has shown that Windows' own tools that capture the screen, such as the Snipping Tool and the Xbox Game Bar, keep working.

## Trade-offs & risks
Windows apps that capture the screen for you, such as screenshot or meeting apps from the Store, cannot do so any more. The switches in Settings stay locked until you undo the change.

## When not to use it
If you take screenshots or share your screen with a Windows app from the Store.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
