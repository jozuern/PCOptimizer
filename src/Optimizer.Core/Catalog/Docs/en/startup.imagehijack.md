# Program redirect (Image File Execution Options, Expert)

## Summary
Removes a "Debugger" redirect that makes Windows start another program whenever this one is launched.

## How it works
Image File Execution Options can name a debugger that Windows starts instead of the program: Microsoft documents the value Debugger under Image File Execution Options\<program name> for that purpose [2][3], and Autoruns lists such entries [1]. Tools such as Process Explorer use it on purpose to replace Task Manager; malware uses it to block or hijack programs. The app deletes only the Debugger value. Undo writes it back.

## Why it can help
If the redirect was not set on purpose, the original program starts normally again.

## Evidence
No effect on games unless a game or launcher was redirected.

## Trade-offs & risks
A replacement you set up on purpose (for example Process Explorer instead of Task Manager) stops working.

## When not to use it
If you set the redirect yourself.

## Sources
1. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/gflags-details
3. https://learn.microsoft.com/en-us/windows/win32/services/debugging-a-service
