# Brave: no product analytics, usage ping and Web Discovery

## Summary
Brave no longer sends privacy-preserving product analytics, the daily usage ping or Web Discovery data for Brave Search.

## How it works
Three Brave policies [1]: BraveP3AEnabled set to 0 stops the anonymous product analytics (P3A); BraveStatsPingEnabled set to 0 stops the lightweight ping used to count active users; BraveWebDiscoveryEnabled set to 0 keeps the Web Discovery Project, which contributes data to the Brave Search index, off [1]. They need Brave 1.83 or later [1].

## Why it can help
Less data leaves the PC, even though Brave describes it as anonymous. It changes neither frame rate nor latency.

## Evidence
Documented Brave policies [1].

## Trade-offs & risks
Brave gets fewer usage numbers to improve the browser. Because these are policies, Brave shows "Managed by your organization" in its menu, and brave://policy lists them. Undo removes them.

## When not to use it
If you want to support Brave with anonymous usage data.

## Sources
1. https://support.brave.app/hc/en-us/articles/360039248271-Group-Policy
