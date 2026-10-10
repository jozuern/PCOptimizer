# File Explorer: no sync provider notifications

## Summary
Hides the information and suggestions File Explorer shows from sync providers (cloud storage apps), like the Folder Options box "Show sync provider notifications".

## How it works
Since Windows 11 build 22572.100, File Explorer can show sync provider notifications with information and suggestions [1]. The box "Show sync provider notifications" is in Folder Options on the View tab [1]. The app sets ShowSyncProviderNotifications to 0. Microsoft does not document the registry value behind the switch; the tutorial [1] shows the value the switch writes. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
No suggestions about new features on top of your folders.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
None known; the box only controls these notifications.

## When not to use it
If you want those hints.

## Sources
1. https://www.elevenforum.com/t/enable-or-disable-sync-provider-notifications-in-file-explorer-in-windows-11.5200/
