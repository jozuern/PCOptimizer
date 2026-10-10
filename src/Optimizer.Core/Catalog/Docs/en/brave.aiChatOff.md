# Brave: no Leo AI assistant

## Summary
Turns off Leo, Brave's built-in AI assistant, in the sidebar and the address bar.

## How it works
The Brave policy BraveAIChatEnabled set to 0 disables Leo: the AI chat button in the sidebar and the address bar integration are gone [1].

## Why it can help
Page content is not sent to the AI assistant by accident. It changes neither frame rate nor latency.

## Evidence
A documented Brave policy [1].

## Trade-offs & risks
Because these are policies, Brave shows "Managed by your organization" in its menu, and brave://policy lists them. Undo removes them.

## When not to use it
If you use Leo.

## Sources
1. https://support.brave.app/hc/en-us/articles/360039248271-Group-Policy
