# Microsoft Defender in a sandbox

## Summary
Runs the part of Microsoft Defender that inspects file contents in a separate sandboxed process, so a flaw in a file parser cannot take over the system. Needs a restart.

## How it works
Microsoft documents the system-wide environment variable MP_FORCE_USE_SANDBOX = 1 followed by a restart; afterwards a content process MsMpEngCP.exe runs next to MsMpEng.exe [1]. Defender has to be in active mode [1].

## Why it can help
Attackers have found ways to exploit Defender's content parsers; in the sandbox such a flaw cannot reach the rest of the system [1].

## Evidence
Documented by Microsoft [1]. Sandboxing runs an additional process next to the antivirus engine [1].

## Trade-offs & risks
One more process runs. Undo removes the variable; after a restart Defender runs without the sandbox again.

## When not to use it
If you use another antivirus; then Defender is not in active mode and the setting has no effect.

## Sources
1. https://learn.microsoft.com/en-us/defender-endpoint/sandbox-mdav
