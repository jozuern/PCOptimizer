# Spectre/Meltdown mitigations off

## Summary
Expert, security trade-off: turns off the OS mitigations for Spectre variant 2 and Meltdown. Mainly affects older CPUs.

## How it works
FeatureSettingsOverride = 3 with FeatureSettingsOverrideMask = 3 disables the Windows mitigations for CVE-2017-5715 and CVE-2017-5754, as documented by Microsoft [1]. Takes effect after a restart.

## Why it can help
The mitigations add work to system calls and context switches. On CPUs without hardware fixes (roughly before 2019), that overhead is larger.

## Evidence
Current CPUs have hardware mitigations; the gain there is usually within run-to-run variance. Treat it as disputed.

## Trade-offs & risks
Exposes the PC to speculative-execution attacks, for example from malicious code in a browser.

## When not to use it
Do not use on current CPUs, on PCs used for banking or work, or if you browse untrusted sites.

## Sources
1. https://support.microsoft.com/en-us/topic/kb4073119-windows-client-guidance-for-it-pros-to-protect-against-silicon-based-microarchitectural-and-speculative-execution-side-channel-vulnerabilities-35820a8a-ae13-1299-88cc-357f104f5b11
