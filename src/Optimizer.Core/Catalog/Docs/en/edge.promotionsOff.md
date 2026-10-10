# Microsoft Edge: no tips, recommendations and Acrobat offer

## Summary
Turns off Edge spotlight backgrounds and tips for Microsoft services, feature recommendations in the browser and the Adobe subscription button in the PDF viewer.

## How it works
SpotlightExperiencesAndRecommendationsEnabled set to off stops customized backgrounds, suggestions, notifications and tips for Microsoft services [1]. ShowRecommendationsEnabled set to off stops the dialogs, flyouts and banners that recommend Edge features [2]. ShowAcrobatSubscriptionButton set to off hides the button in the PDF viewer that offers Adobe subscriptions [3]. The first policy also applies to profiles signed in with a Microsoft account; the other two Microsoft lists as not applied to such profiles since Edge 116 [4], so there Edge keeps your own setting.

## Why it can help
Fewer pop-ups and offers while you browse. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Evidence
Documented Edge policies [1][2][3].

## Trade-offs & risks
Edge no longer points out new features. Because these are policies, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes them.

## When not to use it
If you like Edge's feature tips.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/spotlightexperiencesandrecommendationsenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/showrecommendationsenabled
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/showacrobatsubscriptionbutton
4. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
