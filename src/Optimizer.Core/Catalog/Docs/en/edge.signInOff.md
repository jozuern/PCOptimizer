# Microsoft Edge: no browser sign-in

## Summary
Edge no longer signs in to a Microsoft account, neither by itself with your Windows account nor by hand. Sync of favorites and passwords stops.

## How it works
BrowserSignin set to 0 disables browser sign-in, so account services such as sync and single sign-on are not available [1]. ImplicitSignInEnabled set to off stops Edge from signing in automatically based on how you sign in to Windows [2]. Both apply to all profiles after Edge restarts [1][2].

## Why it can help
Edge stays a local browser without a Microsoft account, even if Windows uses one.

## Evidence
Documented Edge policies [1][2]. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Trade-offs & risks
Favorites, passwords, history and open tabs no longer sync between your devices, and sites that use the Edge sign-in for single sign-on ask you to sign in yourself. Because these are policies, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes them.

## When not to use it
If you sync Edge between devices or use your work account in Edge.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/browsersignin
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/implicitsigninenabled
