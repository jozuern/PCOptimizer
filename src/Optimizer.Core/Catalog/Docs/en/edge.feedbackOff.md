# Microsoft Edge: no feedback tool

## Summary
Removes the Edge feedback feature, so Edge shows no feedback prompts or surveys and cannot send feedback reports.

## How it works
Edge uses its feedback feature for feedback, suggestions, customer surveys and problem reports [1]. With the policy UserFeedbackAllowed set to off, users cannot open Edge Feedback [1]. It applies to all profiles, including ones signed in with a Microsoft account, after Edge restarts [1].

## Why it can help
No survey prompts, and no feedback with screenshots or diagnostic data is sent by mistake. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
A documented Edge policy [1].

## Trade-offs & risks
You can no longer report Edge problems to Microsoft from the browser. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If you send feedback to Microsoft about Edge.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/userfeedbackallowed
