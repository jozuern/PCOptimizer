# Page file managed by Windows

## Summary
Sets the page file back to "Automatically manage paging file size for all drives". Fixes crashes and out-of-memory errors from a page file that was turned off or made too small.

## How it works
The commit limit, the most memory programs can reserve, is RAM plus all page files [1]. Old guides recommend turning the page file off or setting a small fixed size. The app sets the value PagingFiles to ?:\pagefile.sys. This is the value the Windows dialog writes when "Automatically manage paging file size for all drives" is checked; Microsoft does not document the value on its own. Windows then sizes the page file itself, up to 3 times RAM or 4 GB, whichever is larger, and at most an eighth of the drive [2]. Takes effect after a restart.

## Why it can help
When programs reach the commit limit, Windows can freeze, crash or show out-of-memory errors [1]. A managed page file grows when the commit charge gets close to the limit [2].

## Evidence
By default, Windows manages the page file itself [2]. A page file or dedicated dump file is also needed to save a memory dump after a crash [2].

## Trade-offs & risks
Uses disk space on the system drive, up to the limits above.

## When not to use it
Keep a custom size only if you set it on purpose and know the peak commit charge of your workloads. If you already use "System managed size" on drive C:, nothing changes in practice, although the app shows this setting as not applied.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/introduction-to-the-page-file
2. https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/how-to-determine-the-appropriate-page-file-size-for-64-bit-versions-of-windows
