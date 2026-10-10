# Show file extensions

## Summary
Shows file extensions such as .exe or .zip in File Explorer. A usability and safety setting, no performance effect.

## How it works
HideFileExt = 0 shows extensions for known file types, the same as File Explorer > View > Show > File name extensions [1] or Settings > System > Advanced > File Explorer > Show file extensions (called For developers before version 25H2) [2]. Microsoft's Group Policy preferences specification names HideFileExt in Explorer's Advanced settings of the user as the registry value behind this option [3].

## Why it can help
You can see what a file really is, for example a fake "document.pdf.exe".

## Evidence
No performance effect.

## Trade-offs & risks
File names look longer; renaming can change the extension by accident.

## When not to use it
No reason not to use it unless you prefer the shorter names.

## Sources
1. https://support.microsoft.com/en-us/windows/common-file-name-extensions-in-windows-da4a4430-8e76-89c5-59f7-1cdbbc75cb01
2. https://learn.microsoft.com/en-us/windows/advanced-settings/
3. https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-gppref/3c837e92-016e-4148-86e5-b4f0381a757f
