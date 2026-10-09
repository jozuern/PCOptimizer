# Virtualization-based security and memory integrity off

## Summary
Expert, security trade-off: turns off VBS and memory integrity (HVCI). Measured cost about 5 % in CPU-bound games, more on CPUs without MBEC/GMET. Some anti-cheats may require it.

## How it works
VBS uses the Windows hypervisor to run parts of Windows in an isolated environment; memory integrity runs the kernel's code integrity checks inside it [1]. The app sets EnableVirtualizationBasedSecurity = 0 under DeviceGuard and Enabled = 0 under Scenarios\HypervisorEnforcedCodeIntegrity, the value Microsoft uses to turn memory integrity off [1]. It takes effect after a restart. VBS stays on if Group Policy or Intune turns it on, if it was turned on with UEFI lock [1], or if Credential Guard runs, which Windows turns on by default on domain-joined Enterprise and Education PCs since 22H2 [2].

## Why it can help
The hypervisor adds work to some kernel operations. Memory integrity costs less on CPUs with MBEC (Intel 7th generation and newer) or GMET (AMD Zen 2 and newer); older CPUs emulate these features and lose more [1].

## Evidence
Tom's Hardware measured about 3 to 6 % lower average frame rates with VBS or memory integrity on, on four CPUs with MBEC or GMET, at 1080p with an RTX 3090, in 2021 on an early Windows 11 build [3]. The difference depends on resolution and graphics card [3]. FACEIT describes the cost as minor in some cases, mostly on older systems [4].

## Trade-offs & risks
Weakens protection against malware that attacks the Windows kernel, and Windows Security shows a warning while memory integrity is off [1]. Riot Vanguard may ask you to turn security features back on before a game starts [5]. FACEIT requires memory integrity for some players and needs VBS for its IOMMU checks, which it rolls out in waves [4][6]. Undo turns memory integrity back on: a driver installed in the meantime that is not compatible can fail to load or, in rare cases, stop Windows from starting [1]. That is why this counts as a boot-critical change. Microsoft's recovery steps turn memory integrity off again from the recovery environment [1].

## When not to use it
Not if you play games with Riot Vanguard or FACEIT, not on work or school PCs, and not if you cannot judge the security trade-off. Nothing to gain if VBS is already off (System Information > System Summary > Virtualization-based security).

## Sources
1. https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity
2. https://learn.microsoft.com/en-us/windows/security/identity-protection/credential-guard/
3. https://www.tomshardware.com/news/windows-11-gaming-benchmarks-performance-vbs-hvci-security
4. https://support.faceit.com/hc/en-us/articles/23117181142556-Windows-Security-Requirements-FAQ
5. https://support.riotgames.com/riot/performance/vanguard-security-requirements
6. https://support.faceit.com/hc/en-us/articles/8546882512284-Enabling-Memory-Integrity-HVCI
