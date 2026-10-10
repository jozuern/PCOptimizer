# Microsoft Edge: generative AI features off

## Summary
Turns off AI history search, the built-in AI interfaces for websites and AI themes, and keeps Edge from downloading its local AI model; a downloaded model is deleted.

## How it works
EdgeHistoryAISearchEnabled set to off limits history search to exact matches [1]. BuiltInAIAPIsEnabled set to off blocks the LanguageModel, Summarization, Writer and Rewriter APIs for websites [2]. AIGenThemesEnabled set to off turns off themes generated with DALL-E [3]. GenAILocalFoundationalModelSettings set to 1 stops the download of the foundational AI model and deletes an already downloaded model [4]. The model policy also applies to profiles signed in with a Microsoft account; the other three Microsoft lists as not applied to such profiles since Edge 116 [5], so there Edge keeps your own setting.

## Why it can help
Websites cannot run Edge's built-in AI on your PC, and the local model no longer takes disk space.

## Evidence
Documented Edge policies [1][2][3][4]. How large the local model is, Microsoft does not state; this changes neither frame rate nor latency.

## Trade-offs & risks
History search only finds exact words, and sites that use the built-in AI interfaces get an error. Because these are policies, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes them.

## When not to use it
If you use AI history search or sites built on Edge's AI interfaces.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/edgehistoryaisearchenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/builtinaiapisenabled
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/aigenthemesenabled
4. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/genailocalfoundationalmodelsettings
5. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
