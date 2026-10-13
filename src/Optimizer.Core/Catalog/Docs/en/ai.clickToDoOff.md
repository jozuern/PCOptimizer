# Click to Do off

## Summary
Turns off Click to Do, which takes a screenshot of the screen and suggests actions for what is on it. A documented Windows policy for Pro, Enterprise and Education.

## How it works
Click to Do takes a screenshot of the screen when you start it and analyzes it on the PC to offer actions, for example for text or images on the screen [1]. The policy DisableClickToDo set to 1 removes Click to Do and its entry points [1]. Microsoft lists it for Pro, Enterprise and Education [1].

## Why it can help
Nothing on the screen is captured and analyzed by Click to Do, and its entry points stop appearing.

## Evidence
A documented Windows policy [1]. Click to Do exists only on Copilot+ PCs; elsewhere the policy changes nothing you would notice. No published gaming measurement.

## Trade-offs & risks
Click to Do and its quick actions are gone until you undo the change.

## When not to use it
If you use Click to Do.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai
