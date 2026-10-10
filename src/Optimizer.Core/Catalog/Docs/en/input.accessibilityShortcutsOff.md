# No Sticky Keys, Filter Keys and Toggle Keys shortcuts

## Summary
Pressing Shift five times, holding right Shift or holding Num Lock no longer turns on Sticky Keys, Filter Keys or Toggle Keys, and no prompt jumps out of a game.

## How it works
Each of these keyboard accessibility features has a shortcut flag [1][2][3]: Sticky Keys turn on after pressing Shift five times, Filter Keys after holding right Shift for eight seconds, Toggle Keys after holding Num Lock for eight seconds. The app clears only these shortcut flags through SystemParametersInfo and saves them to your user profile [4]; whether a feature itself is on stays as it was. The app changes it only when it runs under your own account, the usual case with the UAC prompt.

## Why it can help
In games that use Shift a lot, five quick presses open the Sticky Keys prompt and can take you out of the game.

## Evidence
Documented Windows interfaces [1][2][3][4]. It stops an interruption; it does not change frame rate or latency.

## Trade-offs & risks
People who need these features turn them on in Settings > Accessibility > Keyboard instead of with the shortcut.

## When not to use it
If you or someone using this PC turns these features on with the keyboard shortcut.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-stickykeys
2. https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-filterkeys
3. https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-togglekeys
4. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
