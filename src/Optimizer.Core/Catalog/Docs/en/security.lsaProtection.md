# LSA protection on

## Summary
Runs the Local Security Authority, which handles sign-in secrets, as a protected process, so other programs cannot read its memory. Takes effect after a restart.

## How it works
With RunAsPPL = 2, Windows 11 22H2 and later run LSASS as a protected process without a UEFI variable, so the setting can be turned off again [1]. In protected mode, only plug-ins signed by Microsoft load into LSA; smart card drivers and other LSA plug-ins must meet Microsoft's signing rules [1]. A restart is needed [1].

## Why it can help
Tools that steal passwords and credentials from LSA memory are blocked.

## Evidence
Documented by Microsoft [1]. Microsoft describes an audit mode to find plug-ins that would fail to load [1]. It changes neither frame rate nor latency.

## Trade-offs & risks
Unsigned LSA plug-ins, such as some smart card, password filter or VPN components, no longer load. Undo and a restart turn the protection off again.

## When not to use it
If you use smart card readers, password filters or other security software that loads into LSA and is not signed by Microsoft.

## Sources
1. https://learn.microsoft.com/en-us/windows-server/security/credentials-protection-and-management/configuring-additional-lsa-protection
