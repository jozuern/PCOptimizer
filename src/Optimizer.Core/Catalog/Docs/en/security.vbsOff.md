# Virtualization-based security and memory integrity off

## Summary
Expert, security trade-off: turns off VBS and memory integrity (HVCI). Blocked while Riot Vanguard or FACEIT is installed.

## How it works
VBS runs parts of Windows in a hypervisor-protected environment; memory integrity uses it to check kernel code before it runs [1]. The app sets the DeviceGuard values that turn both off. It takes effect after a restart. If VBS is also required by Hyper-V, WSL or a UEFI lock, it may stay on.

## Why it can help
The hypervisor adds overhead to some kernel operations. On CPUs without MBEC/GMET (Intel before 7th gen, AMD before Zen 2), memory integrity is emulated and costs more.

## Evidence
Measured differences on current CPUs are a few percent in some games and none in others; on old CPUs without MBEC they are larger.

## Trade-offs & risks
Weakens protection against kernel-level malware. Several anti-cheats (Vanguard, FACEIT) require memory integrity and will refuse to start games. Boot-critical: the app exports the boot configuration first.

## When not to use it
Do not use if you play games with Vanguard or FACEIT, on work PCs, or if you are not comfortable with the security trade-off.

## Sources
1. https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity
