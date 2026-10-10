# Remote Assistance requests off

## Summary
Nobody can ask for or offer help through Windows Remote Assistance invitations on this PC. Quick Assist is a separate app and not affected. Pro, Enterprise and Education.

## How it works
With the policy "Configure Solicited Remote Assistance" disabled (fAllowToGetHelp = 0), users cannot ask for help by email or file transfer, and instant messaging programs cannot allow connections to this PC [1].

## Why it can help
One fewer way for someone to connect to this PC, for example after talking you into sending an invitation.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
Remote Assistance invitations no longer work.

## When not to use it
If someone helps you through Remote Assistance invitations.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-remoteassistance
