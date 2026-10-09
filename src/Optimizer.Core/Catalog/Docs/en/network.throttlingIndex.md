# Network throttling off (NetworkThrottlingIndex)

## Summary
Turns off a packet limit that MMCSS applied to network traffic while audio or video played (documented for Windows Vista). Unverified on Windows 11. Effect disputed.

## How it works
In Windows Vista, the Multimedia Class Scheduler Service (MMCSS) told the network stack to pass on at most 10 received packets per millisecond while multimedia playback ran, so network processing could not interrupt audio [1]. NetworkThrottlingIndex sets this limit; 0xFFFFFFFF turns it off [2]. Current Microsoft documentation of MMCSS does not mention the value [3], so it is unverified whether Windows 11 still applies the limit. Takes effect after a restart.

## Why it can help
If the limit is active, it slows fast transfers that receive many packets per second, for example copying large files over the local network while music plays.

## Evidence
The only Microsoft description is a 2007 article about Windows Vista. It states that internet traffic, even on the best broadband connections of the time, stayed below the limit [1]. We know of no measurements on Windows 11 and no measurements that show an effect on games.

## Trade-offs & risks
Microsoft added the limit because heavy network traffic made audio and video playback glitch in its tests on single-processor PCs [1]. Without it, that can happen again on slow hardware.

## When not to use it
Not needed for online games. If audio crackles during large downloads after applying it, undo it.

## Sources
1. https://learn.microsoft.com/en-us/archive/blogs/markrussinovich/vista-multimedia-playback-and-network-throughput
2. https://support.tibco.com/s/article/Tibco-KnowledgeArticle-Article-38041
3. https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
