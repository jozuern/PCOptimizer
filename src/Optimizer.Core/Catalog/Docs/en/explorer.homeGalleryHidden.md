# File Explorer: no Home and Gallery

## Summary
Removes Home and Gallery from the navigation pane of File Explorer for your account. Values Microsoft does not document, tested.

## How it works
Home and Gallery are shell folders that are pinned to the navigation pane. A per-user copy of their class keys with System.IsPinnedToNameSpaceTree = 0 unpins them for your account [1][2]. Microsoft does not document these values.

## Why it can help
A shorter navigation pane without the entries you do not use.

## Evidence
In a VM test on Windows 11 build 26300, a new File Explorer window showed neither Home nor Gallery after apply and both again after undo. No effect on frame rate or latency.

## Trade-offs & risks
If File Explorer opens to Home, set "File Explorer opens to This PC" as well. Recent files and photos are still in their folders.

## When not to use it
If you use Home or Gallery.

## Sources
1. https://github.com/Raphire/Win11Debloat/blob/master/Regfiles/Hide_Home_from_Explorer.reg
2. https://github.com/Raphire/Win11Debloat/blob/master/Regfiles/Hide_Gallery_from_Explorer.reg
