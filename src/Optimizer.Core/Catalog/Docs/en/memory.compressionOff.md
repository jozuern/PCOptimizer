# Memory compression off

## Summary
Stops Windows from compressing unused memory pages in RAM. Only offered with 16 GB or more. Effect disputed: no published measurement shows a gaming gain.

## How it works
When memory gets tight, Windows compresses unused pages and keeps them in RAM instead of writing them to disk [2]. The compressed pages live in the System process, which is why it can look large in Task Manager [2]. The app turns this off with Disable-MMAgent -MemoryCompression [1]. It takes effect after a restart.

## Why it can help
Compressing and decompressing pages takes some CPU time. With plenty of free RAM, little gets compressed, so the possible saving is small.

## Evidence
Microsoft added compression to keep more data in RAM and improve responsiveness [2]. We found no measurement from Microsoft or a hardware vendor that shows better frame rates or fewer stutters with compression turned off. The 16 GB limit is our own cautious choice, not a published threshold.

## Trade-offs & risks
When memory runs low, pages that would have been compressed are written to the page file instead, and reading them back from disk is slower than decompressing them.

## When not to use it
Do not use it if games or other programs often use most of your RAM.

## Sources
1. https://learn.microsoft.com/en-us/powershell/module/mmagent/disable-mmagent
2. https://blogs.windows.com/windows-insider/2015/08/18/announcing-windows-10-insider-preview-build-10525/
