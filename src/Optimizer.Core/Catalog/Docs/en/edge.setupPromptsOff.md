# Microsoft Edge: no setup and default browser prompts

## Summary
Skips the Edge first run pages, the offer to import data from another browser at each start and the prompts to make Edge and Bing your defaults.

## How it works
Three Edge policies: HideFirstRunExperience hides the first run experience and splash screen [1]; ImportOnEachLaunch set to off stops the offer to import data from the default browser when Edge starts [2]; DefaultBrowserSettingsCampaignEnabled set to off stops the prompts to set Edge as default browser and Bing as default search engine [3]. All three also apply to profiles signed in with a Microsoft account [1][2][3].

## Why it can help
Fewer interruptions when you open Edge, for example on a fresh Windows or after an Edge update.

## Evidence
Documented Edge policies [1][2][3]. It is a privacy or comfort setting and changes neither frame rate nor latency. The first two take effect after Edge restarts [1][2].

## Trade-offs & risks
Edge no longer walks you through its setup; you choose sign-in, sync and import in Edge settings yourself. Because these are policies, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes them.

## When not to use it
If you want Edge's setup pages, for example to import favorites from another browser.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/hidefirstrunexperience
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/importoneachlaunch
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/defaultbrowsersettingscampaignenabled
