# Delete startup entry

## Summary
Deletes this autostart value from the registry, as Autoruns does. Tunero backs it up first, so the Changes page can put it back.

## How it works
Windows runs the command lines in the Run keys every time someone signs in, and the ones in the RunOnce keys once [1]. A Group Policy Run key and the AutoRun value of the Command Processor (run each time cmd.exe starts) work the same way. Turning an entry off keeps it and marks it as disabled; Delete removes the value itself, the way Autoruns does [2]. Before that, Tunero saves the value with its type, so Undo writes it back exactly as it was.

## Why it can help
An entry whose program was uninstalled stays in the list and points to a file that is gone; deleting it tidies the list. RunOnce, policy Run and AutoRun entries cannot be turned off on this page, so Delete is the only way to stop them here.

## Evidence
Nothing measurable: a value that points to a missing file costs almost nothing at sign-in. For entries that still run a program, the effect is the same as turning them off.

## Trade-offs & risks
The program no longer starts by itself, and its own settings may write the entry again the next time you change them. A RunOnce entry an installer left for the next sign-in (finishing an update) does not run; restore it on the Changes page if the program then misbehaves.

## When not to use it
For entries you only want to pause: turn them off instead, which keeps them in the list. For RunOnce entries of an installer you just ran: let it finish with a restart first.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys
2. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
