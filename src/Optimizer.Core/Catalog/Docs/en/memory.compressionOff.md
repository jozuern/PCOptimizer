# Memory compression off

## Summary
Stops Windows from compressing memory pages in RAM. Only offered with 16 GB or more. Effect disputed.

## How it works
When memory gets tight, Windows compresses rarely used pages in RAM instead of writing them to the page file. The app turns this off with Disable-MMAgent -MemoryCompression [1]; it takes effect after a restart.

## Why it can help
Compression costs CPU time when pages are compressed and decompressed. With plenty of RAM, little gets compressed anyway, and that CPU time is saved.

## Evidence
Measurements show no consistent gaming difference on PCs with enough RAM. With little RAM, turning it off can make things worse, which is why it is not offered below 16 GB.

## Trade-offs & risks
More page-file use when memory gets full.

## When not to use it
Do not use with 8 GB or less.

## Sources
1. https://learn.microsoft.com/en-us/powershell/module/mmagent/disable-mmagent
