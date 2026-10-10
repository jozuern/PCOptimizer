# Alt+Tab: open windows only

## Summary
Alt+Tab shows only open windows, without browser tabs from Microsoft Edge. Pro and up; Microsoft marks the policy as a preview.

## How it works
The policy BrowserAltTabBlowout controls whether app tabs appear in Alt+Tab [1]. The value "Open windows only" turns the feature off [1]. The app sets MultiTaskingAltTabFilter to 4 in the user policy key. Microsoft notes that the policy is in preview and meant for testing [1], so the tweak is a Preview too.

## Why it can help
Switching between a game and other windows with Alt+Tab is quicker when Edge tabs do not fill the list.

## Evidence
Documented for Pro, Enterprise and Education from Windows 11 21H2 [1]. No effect on frame rate or latency.

## Trade-offs & risks
Edge tabs no longer show up as separate entries in Alt+Tab; you switch tabs inside Edge.

## When not to use it
If you jump between Edge tabs with Alt+Tab.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-multitasking
