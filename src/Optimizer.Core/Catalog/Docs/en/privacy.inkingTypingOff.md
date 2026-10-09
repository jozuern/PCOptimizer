# Inking and typing personalization off

## Summary
Stops Windows from learning from your typing and handwriting and from sending inking and typing data to Microsoft.

## How it works
The policy "Turn off automatic learning" stops Windows from collecting typed text and handwriting for your personal dictionary and deletes what was stored [3]. The policy "Improve inking and typing recognition" is turned off, so inking and typing data is no longer sent to Microsoft [1][2]. Two user switches stop collecting contacts for suggestions and turn off typing insights; Microsoft does not document these two values.

## Why it can help
Less data leaves this PC. No measurable effect on performance.

## Evidence
Privacy setting; it does not change frame rate or latency.

## Trade-offs & risks
Text suggestions and handwriting recognition learn less from you, and the personal dictionary Windows built so far is deleted.

## When not to use it
If you rely on handwriting with a pen.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-textinput
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
3. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-globalization
