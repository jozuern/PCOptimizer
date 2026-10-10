# No privacy settings page at sign-in

## Summary
Windows no longer shows its privacy settings page at sign-in, for example after a feature update.

## How it works
The policy DisablePrivacyExperience = 1 stops the privacy settings experience from launching when a user signs in [1].

## Why it can help
Feature updates do not stop you with the privacy page again, where clicking through quickly can turn options back on.

## Evidence
A documented Windows policy [1]. A privacy setting without effect on frame rate or latency.

## Trade-offs & risks
You review privacy options in Settings > Privacy & security yourself.

## When not to use it
If you want Windows to ask about privacy options after updates.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
