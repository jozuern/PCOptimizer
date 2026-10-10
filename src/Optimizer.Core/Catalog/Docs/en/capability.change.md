# Add or remove a Windows capability

## Summary
Adds or removes this Windows capability (a Feature on Demand) with DISM, like Settings > System > Optional features. Adding one downloads it from Windows Update.

## How it works
Capabilities are parts of Windows that are installed separately, such as PowerShell ISE or the OpenSSH client [1]. The app runs DISM with /Remove-Capability or /Add-Capability; adding looks for the package on Windows Update [2]. Undo adds or removes it again.

## Why it can help
Removing tools you do not use frees a few megabytes and takes them out of Start and the command line. Adding one back (for example WMIC for an old script) needs no installer from elsewhere.

## Evidence
Documented by Microsoft [1][2]. Capabilities do not run in the background, so removing them changes neither frame rate nor latency.

## Trade-offs & risks
Programs or scripts that call a removed tool stop working until you add it again. Adding needs an internet connection to Windows Update.

## When not to use it
If you use the tool, or an old script or installer calls it.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/features-on-demand-non-language-fod
2. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-capabilities-package-servicing-command-line-options
