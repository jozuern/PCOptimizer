# Update restarts when you choose, for up to a week

## Summary
After a monthly update, Windows does not restart by itself until 7 days after it found the update (at least 2 days after installing it). You pick the time. Pro, Enterprise and Education.

## How it works
Without a policy, Windows installs an update and restarts by itself outside of active hours [3]. The policy "Specify deadline for automatic updates and restarts for quality update" sets a deadline and a grace period [1][2]. The app sets SetComplianceDeadlineForQU = 1, ConfigureDeadlineForQualityUpdates = 7, ConfigureDeadlineGracePeriod = 2 and ConfigureDeadlineNoAutoRebootForQualityUpdates = 1, the option "Don't auto-restart until end of grace period" [1]. Updates then install as soon as Windows offers them, and Windows waits with the restart until the later of 7 days after it found the update and 2 days after installing it [2]. Until then it reminds you and lets you pick a time [2].

## Why it can help
An automatic restart at night can end a long download, a render or a game left running. With this policy nothing restarts unless you choose, for about a week.

## Evidence
A documented Windows Update policy for Windows 11 22H2 and later [1][2]. No effect on frame rate or latency.

## Trade-offs & risks
Once the deadline has passed, Windows restarts regardless of active hours, after a 15 minute warning [2]: restart yourself within the week to avoid that. Security fixes only take effect after the restart. The policy also starts the installation as soon as an update is offered [2]. Undo removes the policy.

## When not to use it
If you prefer Windows to restart by itself at night, or never restart on your own for weeks.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
2. https://learn.microsoft.com/en-us/windows/deployment/update/wufb-compliancedeadlines
3. https://learn.microsoft.com/en-us/windows/deployment/update/waas-restart
