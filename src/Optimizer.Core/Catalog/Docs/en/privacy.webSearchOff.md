# Web results in Start search off

## Summary
Start menu search shows only results from this PC. Microsoft documents this policy only for File Explorer, so the effect on Start is not confirmed by Microsoft.

## How it works
The user policy "Turn off display of recent search entries in the File Explorer search box" is turned on [1]. Microsoft documents it only for File Explorer, where recent searches are no longer shown or saved. On Windows 11 it also removes web results and suggestions from the Start menu search, which Microsoft does not document. The documented policy "Don't search the web or display web results in Search" works only on Enterprise and Education [2].

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
You search the web in the browser instead of the Start menu. File Explorer no longer suggests your earlier searches.

## When not to use it
If you use the Start menu to search the web.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowsexplorer
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search
