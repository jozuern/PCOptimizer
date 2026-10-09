# Background apps off

## Summary
Stops Store apps from running in the background. Desktop programs are not affected. Effect on games is disputed and usually zero.

## How it works
The policy Let Windows apps run in the background is set to Force deny for all Store apps [2]. Windows 11 otherwise manages this per app under Settings > Apps > Installed apps > Advanced options [1].

## Why it can help
Fewer Store apps waking up in the background.

## Evidence
Desktop programs such as Steam, Discord and game launchers are not affected [1]. No published measurement shows a frame rate gain.

## Trade-offs & risks
Communication apps may stop showing notifications and syncing in the background, as Microsoft warns [2]. The per-app setting in Settings is locked while the policy is set.

## When not to use it
Keep background apps if you rely on notifications from Store apps such as Phone Link.

## Sources
1. https://support.microsoft.com/en-us/windows/windows-background-apps-and-your-privacy-83f2de44-d2d9-2b29-4649-2afe0913360a
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
