# Microsoft Edge: no startup boost, no background mode

## Summary
Stops Microsoft Edge from starting at sign-in and from running on after its last window is closed. Frees memory and processor time when you are not using Edge.

## How it works
Two documented Edge policies [1][2]: startup boost (StartupBoostEnabled) starts Edge processes at sign-in and restarts them in the background after the last window closes; background mode (BackgroundModeEnabled) keeps Edge running after closing. The app sets both to off. Edge applies policy changes while it runs.

## Why it can help
Edge processes that run without an open window take memory and some processor time, also during games and on battery. On PCs with little memory that leaves more for the apps you use.

## Evidence
Both policies are documented by Microsoft [1][2]. Without the policies, startup boost may be on or off depending on the setup, and background mode is off unless you turned it on [1][2]. How much memory the idle processes take depends on extensions and open sessions.

## Trade-offs & risks
The first Edge window after sign-in opens a little slower. Extensions and web apps no longer run with Edge closed. Because these are policies, Edge shows "Your browser is managed by your organization" in its menu and settings; Undo removes them.

## When not to use it
If you open Edge right after every sign-in and want it as fast as possible, or rely on extensions that work with Edge closed.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-browser-policies/startupboostenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-browser-policies/backgroundmodeenabled
