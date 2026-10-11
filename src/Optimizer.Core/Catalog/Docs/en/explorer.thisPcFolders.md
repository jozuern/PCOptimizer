# File Explorer: user folders in This PC

## Summary
Shows Desktop, Documents, Downloads, Music, Pictures and Videos in This PC again. Values Microsoft does not document, tested.

## How it works
The user folders have entries under MyComputer\NameSpace, and HiddenByDefault decides whether This PC shows them. The app sets HiddenByDefault = 0 for the six folders, the value Win11Debloat uses to show them again [1]. The change applies to all accounts. Microsoft does not document these values.

## Why it can help
Your folders are one click away in This PC, as in Windows 10.

## Evidence
In a VM test on Windows 11 build 26300, This PC listed only the drives before, the six folders above them after apply, and only the drives again after undo. No effect on frame rate or latency.

## Trade-offs & risks
This PC gets longer. The folders stay in the navigation pane either way.

## When not to use it
If you prefer This PC with drives only.

## Sources
1. https://github.com/Raphire/Win11Debloat/blob/master/Regfiles/Add_All_Folders_Under_This_PC.reg
