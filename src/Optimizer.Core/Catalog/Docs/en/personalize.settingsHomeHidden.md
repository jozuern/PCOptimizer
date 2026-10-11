# Settings: no Home page

## Summary
Hides the Home page of the Settings app with the documented Settings Page Visibility policy, so Settings opens on another page. Pro, Enterprise and Education.

## How it works
The policy Settings Page Visibility hides the Settings pages it lists; the value hide:home hides the page with the address ms-settings:home [1]. Microsoft lists the policy for Pro, Enterprise and Education [1]. The value is SettingsPageVisibility under SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer.

## Why it can help
Settings opens without the Home page and its cards, straight on a settings page.

## Evidence
Documented policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
If another program or an administrator already uses this policy to hide pages, the tweak replaces that list; undo restores it.

## When not to use it
If you use the Home page of Settings.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-settings
