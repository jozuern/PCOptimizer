# Program Compatibility Assistant off

## Summary
Windows no longer watches the programs you run for compatibility problems and no longer offers compatibility fixes after a program fails.

## How it works
The Program Compatibility Assistant monitors applications the user runs and, when it detects a potential compatibility issue, offers solutions [1]. The policy "Turn off Program Compatibility Assistant" (DisablePCA = 1) turns it off [1].

## Why it can help
One fewer background monitor; with it off, old programs are not changed by compatibility fixes you did not choose.

## Evidence
A documented Windows policy [1]. No published measurement of an effect on games.

## Trade-offs & risks
Old programs that fail no longer get offered compatibility settings; you set them in the program's properties yourself.

## When not to use it
If you run many old programs and use the compatibility suggestions.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-appcompat
