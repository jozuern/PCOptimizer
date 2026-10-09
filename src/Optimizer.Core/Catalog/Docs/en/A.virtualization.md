# Hardware virtualization off in the BIOS

## Summary
::: variant antiCheat
Hardware virtualization is off in the BIOS. {{antiCheats}} can ask for memory integrity, which needs it, so games may refuse to start.
:::
::: variant off
Hardware virtualization (Intel VT-x or AMD-V) is off in the BIOS. Memory integrity and Linux or Android apps on Windows need it.
:::
::: variant default
Checks that hardware virtualization (Intel VT-x or AMD-V) is on in the BIOS. Memory integrity needs it.
:::

## Why it matters
Memory integrity (core isolation) works in an isolated environment created with hardware virtualization. To use it, virtualization must be enabled in the UEFI or BIOS [1]. Some anti-cheats check memory integrity: Riot Vanguard can ask for it on specific PCs [2], and FACEIT enforces it in rollout waves [3]. Virtualization also lets Windows run other operating systems, such as Linux or Android apps [4].

Turning virtualization on in the BIOS does not change game performance by itself. Whether you then turn memory integrity on is a separate choice.

## How we detected it
We read from Windows (WMI) whether the processor supports virtualization (`Win32_Processor.VMMonitorModeExtensions`), whether the firmware turned it on (`VirtualizationFirmwareEnabled`) [5] and whether Windows runs its hypervisor (`Win32_ComputerSystem.HypervisorPresent`) [6]. When the Windows hypervisor already runs, the processor flags can read as off, so a running hypervisor counts as "on".

## How to fix
1. Restart into the BIOS (press **Del** or **F2** during start).
::: if intel
2. Turn on **Intel Virtualization Technology (VT-x)**, usually on the CPU or Advanced page [4].
:::
::: ifnot intel
2. Turn on **AMD Virtualization (AMD-V)**, usually on the CPU or Advanced page [4]. The menu name differs between boards; search for "virtualization".
:::
3. Save and exit (usually **F10**).

## How to check the fix
Run the scan again. "Virtualization (Intel VT-x or AMD-V) on" should show "Yes".

## Sources
1. https://support.microsoft.com/en-us/windows/security/windows-security/device-security-in-the-windows-security-app
2. https://support.riotgames.com/riot/performance/vanguard-security-requirements
3. https://support.faceit.com/hc/en-us/articles/23117181142556-Windows-Security-Requirements-FAQ
4. https://support.microsoft.com/en-us/windows/experience/enable-virtualization-on-windows
5. https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-processor
6. https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-computersystem
