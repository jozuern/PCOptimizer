# AutoPlay off on all drives

## Summary
Inserted USB sticks, discs and memory cards no longer start programs or open a prompt by themselves.

## How it works
AutoPlay reads a drive as soon as media is inserted, so setup programs and music start right away [1]. The policy "Turn off AutoPlay" set for all drives (NoDriveTypeAutoRun = 255) disables it on all drives [1].

## Why it can help
A USB stick from someone else cannot start something on its own when you plug it in.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
You open new drives in File Explorer yourself; the AutoPlay choices in Settings no longer apply.

## When not to use it
If you like the prompt when you insert a camera card or disc.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-autoplay
