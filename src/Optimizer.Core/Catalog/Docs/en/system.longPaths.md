# Long file paths

## Summary
Lets apps that declare support use file paths longer than 260 characters. Helps with deep project folders, for example in development tools and mod managers.

## How it works
Windows limits paths to 260 characters unless LongPathsEnabled = 1 is set and the app declares long path support in its manifest [1][2]. Each process reads the value once, so a restart makes all apps see it [1].

## Why it can help
Copying or unpacking deeply nested folders no longer fails with path too long errors in apps that support long paths.

## Evidence
Documented by Microsoft [1][2]. No effect on frame rate or latency.

## Trade-offs & risks
Apps without long path support still cannot use such paths [1].

## When not to use it
If you never work with deeply nested folders.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-filesys
