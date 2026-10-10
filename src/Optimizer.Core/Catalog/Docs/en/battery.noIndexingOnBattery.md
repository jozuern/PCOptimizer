# No search indexing on battery

## Summary
The Windows Search indexer pauses while the laptop runs on battery and continues on mains power.

## How it works
The policy "Prevent indexing when running on battery power to conserve energy" pauses the indexer while the computer runs on battery; without it, indexing follows the default behavior [1]. Microsoft notes that since Windows Vista the indexer already backs off on low power [1].

## Why it can help
Indexing new files reads the drive and uses the processor; on battery that work waits for the charger.

## Evidence
A documented Windows Search policy [1]. How much battery it saves depends on how many files change; Microsoft publishes no number.

## Trade-offs & risks
Files added on battery show up in search only after the laptop is plugged in.

## When not to use it
If you need new files in search results right away while on battery.

## Sources
1. https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2008-R2-and-2008/cc732491(v=ws.10)
