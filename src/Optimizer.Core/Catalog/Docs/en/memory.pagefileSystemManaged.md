# Page file managed by Windows

## Summary
Sets the page file back to "Automatically manage". Fixes crashes and out-of-memory errors from a page file that was turned off or made too small.

## How it works
The page file extends RAM and is needed for crash dumps. Old guides recommend turning it off or setting a small fixed size. The value PagingFiles = ?:\pagefile.sys means Windows manages size and location on all drives [1]. Takes effect after a restart.

## Why it can help
Games with large memory use can crash or stutter when the commit limit is reached; a managed page file grows when needed.

## Evidence
Microsoft recommends a system-managed page file for most systems [1]. With enough RAM, the page file is rarely used and costs nothing.

## Trade-offs & risks
Uses some disk space on the system drive.

## When not to use it
Keep a custom size only if you set it on purpose and know the commit peak of your workloads.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/introduction-to-the-page-file
