# Open with: no Store search and no new app notices

## Summary
Removes "Look for an app in the Store" from the Open with dialog and stops the notice that a new app can open a file type.

## How it works
ShellNoUseStoreOpenWith (NoUseStoreOpenWith = 1) removes the "Look for an app in the Store" item from the Open with dialog for file types without an app [1]. NoNewAppAlert = 1 removes the notification that a newly installed app can handle a file type or protocol [2].

## Why it can help
Fewer prompts after installing apps, and no Store page when you open an unknown file by mistake.

## Evidence
Documented Windows policies [1][2]. No effect on frame rate or latency.

## Trade-offs & risks
For unknown file types you choose an installed app yourself, and a new app does not tell you it can open your files.

## When not to use it
If you look for apps in the Store from the Open with dialog.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-icm
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowsexplorer
