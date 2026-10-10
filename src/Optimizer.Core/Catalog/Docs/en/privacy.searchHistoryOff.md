# Search: no history on this device

## Summary
Windows Search no longer saves your searches on this PC, like the switch "Search history on this device". Existing history is not deleted.

## How it works
Windows Search saves your search history on the device to find things faster, for example by ranking an app higher if you searched for it before [1]. The switch is in Settings > Privacy & security > Search permissions [1]. The app sets IsDeviceSearchHistoryEnabled to 0. Microsoft does not document the registry value behind the switch; the tutorial [2] shows the value the switch writes. A test on real Windows 11 26H2 confirmed the effect, and undo removed it again.

## Why it can help
Other people using this account do not see what you searched for.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
Search no longer ranks results by your past searches. Saved history stays until you select "Clear device search history" [1].

## When not to use it
If you like search to remember what you open often.

## Sources
1. https://support.microsoft.com/en-us/windows/privacy/windows-search-and-privacy
2. https://www.elevenforum.com/t/enable-or-disable-recent-search-history-in-windows-11.5395/
