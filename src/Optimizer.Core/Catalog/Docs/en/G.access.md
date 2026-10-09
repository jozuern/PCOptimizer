# Game access: anti-cheat requirements

## Summary
::: status Problem
An installed anti-cheat ({{antiCheats}}) needs a security feature that is off on this PC. Its games will not start until it is on.
:::
::: status Info
::: variant sometimes
All required features are on. {{antiCheats}} can ask for more in some games or modes (memory integrity or IOMMU).
:::
::: variant baseline
UEFI boot, Secure Boot or TPM 2.0 is missing. Games with strict anti-cheats (Valorant, FACEIT, Battlefield 6) will not start.
:::
:::
::: status Ok
The security features required by the installed anti-cheats are on.
:::
::: status Unknown,Unsupported
Shows the security features that kernel anti-cheats check before a game starts. This is about access to games, not FPS.
:::

## Why it matters
Several current anti-cheats only allow play on PCs with a verified boot chain. If a required feature is missing, the game shows an error and does not start. That is why this app never recommends turning these features off for performance and blocks such changes while a strict anti-cheat is installed.
::: if vanguard
**Riot Vanguard** (Valorant, League of Legends) requires TPM 2.0 and Secure Boot on Windows 11. It can additionally ask for memory integrity (HVCI) and IOMMU on specific PCs, for example after a detection [1].
:::
::: if faceit
**FACEIT** requires TPM 2.0 and Secure Boot, and enforces memory integrity and IOMMU in rollout waves for more and more players [2].
:::
::: if javelin
**EA Javelin** (Battlefield 6) requires Secure Boot. TPM 2.0 is required for some games, such as Battlefield 6 [3].
:::

## How we detected it
We read the firmware type (UEFI or legacy), the Secure Boot state, the TPM version, the state of virtualization-based security and memory integrity, IOMMU support (from Kernel DMA Protection) and the system disk's partition style. Installed anti-cheats are recognized by their service and driver registration, so Vanguard is also found in on-demand mode. IOMMU can be on without Kernel DMA Protection, so "unknown" there is not a problem.

## How to fix
::: if mbr
1. **UEFI and GPT:** the system disk uses MBR. Convert it with `mbr2gpt /validate` and then `mbr2gpt /convert /allowFullOS` before switching the BIOS to UEFI only (CSM off). Back up first.
:::
::: ifnot mbr
1. **UEFI:** in the BIOS, disable **CSM** (legacy boot) so the PC starts in pure UEFI mode.
:::
2. **Secure Boot:** in the BIOS boot or security page, set Secure Boot to **Enabled** (OS type: Windows UEFI mode). Some boards need "Install default Secure Boot keys" first.
3. **TPM 2.0:** enable **Intel PTT** or **AMD fTPM** in the BIOS.
4. **Memory integrity:** Windows Security > Device security > Core isolation > Memory integrity. Restart afterwards.
5. **IOMMU:** in the BIOS, enable **VT-d** (Intel) or **IOMMU** / **AMD-Vi** (AMD).

## How to check the fix
Run the scan again. Each required item should show "Yes" or "Running". The anti-cheat's own error code page lists the exact requirement it checks.

## Sources
1. https://support.riotgames.com/riot/performance/vanguard-security-requirements
2. https://support.faceit.com/hc/en-us/articles/23117181142556-Windows-Security-Requirements-FAQ
3. https://www.thesixthaxis.com/2025/08/07/battlefield-6-and-black-ops-7-both-need-secure-boot-and-tpm-2-0-for-pc-anti-cheat/
