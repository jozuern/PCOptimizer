# File Explorer opens to This PC

## Summary
File Explorer opens to This PC (drives and folders) instead of Home (recent files and recommendations).

## How it works
The app sets the per-user value behind "Open File Explorer to" in the folder options (LaunchTo = 1, This PC) [1][2]. New Explorer windows use it right away.

## Why it can help
Home lists recent and recommended files, which can take a moment to load with many files or cloud accounts. This PC opens straight to drives and folders. It also keeps recent file names off the screen, for example when sharing your screen.

## Evidence
A standard folder option; the difference in loading time depends on how many recent files and cloud accounts Home has to show.

## Trade-offs & risks
The recent files list no longer greets you; it stays available under Home in the navigation pane.

## When not to use it
If you mostly reopen recent files through Home.

## Sources
1. https://www.winhelponline.com/blog/open-file-explorer-downloads-folder-default-windows-10/
2. https://www.elevenforum.com/t/change-folder-to-open-file-explorer-to-by-default-in-windows-11.675/
