# Microsoft Edge: no saving of passwords

## Summary
Edge no longer offers to save passwords. Passwords you saved before stay usable. Edge ignores it in profiles signed in with a personal Microsoft account.

## How it works
With the policy PasswordManagerEnabled set to off, users cannot save or add new passwords in Edge, but can still use previously saved passwords [1]. Since Edge 116 Microsoft lists this policy as not applied to a profile that is signed in with a personal Microsoft account [2]: in such a profile Edge keeps your own setting, even though the app shows the change as made.

## Why it can help
Useful if you keep your passwords in a separate password manager and do not want Edge to ask.

## Evidence
A documented Edge policy [1]. It is a privacy or comfort setting and changes neither frame rate nor latency.

## Trade-offs & risks
New passwords are not saved in Edge, so you need another password manager. Because this is a policy, Edge shows in its menu and settings that it is managed by your organization, and the matching switch in Edge settings is locked. Undo removes it.

## When not to use it
If Edge is your password manager.

## Sources
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/passwordmanagerenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
