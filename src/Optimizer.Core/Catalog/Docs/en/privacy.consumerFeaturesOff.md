# Consumer features off

## Summary
Stops Windows from installing promoted apps and showing suggested content from the Store, for example in Start.

## How it works
The policy DisableWindowsConsumerFeatures = 1 turns off the consumer experience that adds suggested apps and tips [1].

## Why it can help
Avoids unwanted apps that install and update in the background.

## Evidence
No direct frame-rate effect.

## Trade-offs & risks
On Home and Pro editions, Windows may not honor every part of this policy.

## When not to use it
No reason not to use it if you do not want suggested apps.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience
2. https://github.com/ChrisTitusTech/winutil
