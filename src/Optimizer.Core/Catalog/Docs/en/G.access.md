# Game access: anti-cheat requirements

## Summary
::: status Problem
An installed anti-cheat ({{antiCheats}}) needs a security feature that is off on this PC. Its games will not start until it is on.
:::
::: status Info
::: variant sometimes
All required features are on. {{antiCheats}} can ask for more in some games, modes or accounts, such as UEFI boot, Secure Boot, TPM 2.0, VBS, memory integrity or IOMMU. The details show what is off.
:::
::: variant baseline
UEFI boot, Secure Boot or TPM 2.0 is missing. Games with strict anti-cheats (Valorant, FACEIT, Battlefield 6, Call of Duty) will not start.
:::
:::
::: status Ok
::: variant none
No kernel anti-cheat found. UEFI boot, Secure Boot and TPM 2.0 are on, the minimum that games with strict anti-cheats require.
:::
::: variant default
Every security feature that the installed anti-cheats require is on.
:::
:::
::: status Unknown,Unsupported
Shows the security features that kernel anti-cheats check before a game starts. This is about access to games, not FPS.
:::

## Why it matters
Several current anti-cheats only allow play on PCs with a verified boot chain. If a required feature is missing, the game shows an error and does not start. That is why this app never recommends turning these features off for performance, and asks for confirmation before a change that turns them off while a strict anti-cheat is installed. Call of Duty (Black Ops 7, Warzone, Modern Warfare 4) also requires TPM 2.0 and Secure Boot with UEFI boot [11].
::: if vanguard
**Riot Vanguard** (Riot games) requires TPM 2.0 and Secure Boot [2][3]. Depending on the PC it can also ask for memory integrity (HVCI) and IOMMU before a game starts [1]. When Vanguard reports VAN: RESTRICTION, it needs the firmware TPM (Intel PTT or AMD fTPM); a separate TPM module is not enough there [3].
:::
::: if faceit
**FACEIT** requires UEFI boot, TPM 2.0 and Secure Boot from all players since 25 November 2025 [4]. IOMMU together with virtualization-based security (VBS) is enforced in waves, already for many players and for everyone above 3,000 Elo [4][5]. Some players are also asked to turn on memory integrity [6].
:::
::: if javelin
**EA Javelin** can require Secure Boot in some EA games. EA introduced configurable Secure Boot requirements starting with Battlefield 2042, and Battlefield 6 requires Secure Boot and TPM 2.0 [7][8].
::: if unverified_javelin
The service name we use to detect Javelin is not confirmed yet, so this entry is marked as not verified.
:::
:::
::: if eac
**Easy Anti-Cheat** has no fixed requirements. Each game's developer can require Secure Boot, TPM or IOMMU for some modes, some accounts or all players [9]. On Windows Insider and Beta builds, games that use it through Epic Online Services require Secure Boot, TPM and memory integrity [10].
:::

## How we detected it
We read the firmware type (UEFI or legacy), the Secure Boot state, the TPM version, the state of virtualization-based security and memory integrity (`Win32_DeviceGuard`) [14], IOMMU support (the DMA protection that Windows reports for virtualization-based security) [14] and the system disk's partition style. Installed anti-cheats are recognized by their service and driver registration, so Vanguard is also found in on-demand mode. IOMMU can be on without Windows reporting DMA protection, so "unknown" there is not a problem. When Windows cannot report the state of virtualization-based security, memory integrity counts as unknown, not as off.

## How to fix
1. **BitLocker:** if BitLocker or device encryption is on, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update or a change to the TPM or the boot configuration can make Windows ask for it at the next start [15][16].
::: if mbr
2. **UEFI and GPT:** the system disk uses MBR. In a command prompt run as administrator, check it with `mbr2gpt /validate /allowFullOS` and then convert it with `mbr2gpt /convert /allowFullOS`. BitLocker protection must be suspended for this. Afterwards switch the BIOS to UEFI only (CSM off), or Windows will not start [12]. Back up your data first.
:::
::: ifnot mbr
2. **UEFI:** in the BIOS, disable **CSM** (legacy boot) so the PC starts in pure UEFI mode.
:::
3. **Secure Boot:** in the BIOS boot or security page, set Secure Boot to **Enabled** (on ASUS boards: OS Type **Windows UEFI mode**). Some boards need "Install default Secure Boot keys" first.
4. **TPM 2.0:** enable **Intel PTT** or **AMD fTPM** in the BIOS, often under "Security" or "Trusted Computing" [5].
5. **Virtualization:** enable **Intel Virtualization Technology (VT-x)** or **SVM Mode** (AMD) in the BIOS. Memory integrity and VBS need it [6][13].
6. **Memory integrity and VBS:** Windows Security > Device security > **Core isolation details** > **Memory integrity**, then restart [13][14]. Memory integrity is a VBS feature, so this also turns on VBS [14]. Some drivers are not compatible with memory integrity and can cause errors or, rarely, a blue screen [14].
7. **IOMMU:** in the BIOS, enable **VT-d** (Intel) or **IOMMU** / **AMD-Vi** (AMD) [5].

## How to check the fix
Run the scan again. Each required item should show "Yes" or "Running". The anti-cheat's own error code page lists the exact requirement it checks.

## Sources
1. https://support.riotgames.com/riot/performance/vanguard-security-requirements
2. https://support.riotgames.com/en-us/riot/client/error-van-9003/
3. https://support.riotgames.com/en-us/riot/client/enable-tpm-20
4. https://support.faceit.com/hc/en-us/articles/23117375791772-How-can-I-check-if-I-need-to-make-any-changes-to-my-PC-before-using-the-FACEIT-Anti-Cheat
5. https://support.faceit.com/hc/en-us/articles/23117181142556-Windows-Security-Requirements-FAQ
6. https://support.faceit.com/hc/en-us/articles/22851956652956-Known-issues-with-Anti-Cheat-Requirements
7. https://www.ea.com/games/battlefield/battlefield-6/news/secure-boot-information
8. https://www.ea.com/news/ea-javelin-anticheat-2026-update
9. https://www.easy.ac/support/articles/additional-security-requirements
10. https://dev.epicgames.com/docs/epic-online-services/trust-and-safety/anti-cheat-interfaces/anti-cheat-interfaces
11. https://support.activision.com/articles/trusted-platform-module-and-secure-boot
12. https://learn.microsoft.com/en-us/windows/deployment/mbr-to-gpt
13. https://support.microsoft.com/en-us/windows/security/windows-security/device-security-in-the-windows-security-app
14. https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity
15. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
16. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
