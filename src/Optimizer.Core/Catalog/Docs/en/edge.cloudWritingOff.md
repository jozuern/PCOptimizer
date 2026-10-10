# Microsoft Edge: no cloud text prediction and spell checking

## Summary
Text in web forms no longer goes to Microsoft services for predictions and enhanced spell checking; local spell checking stays. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
TextPredictionEnabled set to off stops the predictions the Microsoft Turing service generates for long text fields [1]. MicrosoftEditorProofingEnabled set to off leaves spell checking to local engines instead of the Microsoft Editor service [2]. Since Edge 116 Microsoft lists these policies as not applied to a profile that is signed in with a personal Microsoft account [3]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
Text you write in web forms stays on the PC. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
Documented Edge policies [1][2]. Microsoft notes that local spell checking can give less detailed results [2].

## Trade-offs & risks
No word predictions and simpler spelling suggestions. Because these are policies, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes them.

## When not to use it
If you use Edge's text predictions or grammar suggestions.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/textpredictionenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/microsofteditorproofingenabled
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
