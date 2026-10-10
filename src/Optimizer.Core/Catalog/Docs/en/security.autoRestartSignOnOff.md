# No automatic sign-in after restarts

## Summary
After a restart or an update, Windows no longer signs you in automatically and locks the session; you sign in yourself. Pro, Enterprise and Education.

## How it works
By default Windows signs in the last user automatically and locks the session after a restart or shutdown, if that user did not sign out [1]. On PCs not joined to a domain this applies to update restarts and your own restarts [1]. The app sets the policy to off (DisableAutomaticRestartSignOn = 1) [1].

## Why it can help
Apps from your account do not start in the background before you are at the PC.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
After an update restart, apps that reopen at sign-in wait until you sign in yourself.

## When not to use it
If you like your apps to be ready after an update restart.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowslogon
