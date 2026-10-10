# Detailed status messages at startup and shutdown

## Summary
Windows shows what it is doing during startup, shutdown, sign-in and sign-out, for example which service it waits for, instead of "Please wait".

## How it works
The policy "Display highly detailed status messages" (VerboseStatus = 1) directs the system to display highly detailed status messages; Microsoft designed it for advanced users who need this information [1].

## Why it can help
When startup or shutdown hangs, the message shows which step takes so long.

## Evidence
A documented Windows policy [1]. It changes only the text on screen; no effect on frame rate or latency.

## Trade-offs & risks
Technical messages instead of the plain ones.

## When not to use it
If the detailed messages confuse other people who use this PC.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-logon
