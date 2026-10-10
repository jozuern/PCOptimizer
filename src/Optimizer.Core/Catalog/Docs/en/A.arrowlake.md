# Intel Core Ultra 200S: BIOS update with microcode {{min}}

## Summary
::: status Problem
Your BIOS provides microcode older than {{min}}. Intel's later updates for Core Ultra 200S fix performance issues in games.
:::
::: status Ok
Your BIOS provides microcode {{min}} or newer with Intel's game performance fixes for Core Ultra 200S.
:::
::: status Unknown,Info,Unsupported
Checks that Core Ultra 200S desktop CPUs run BIOS microcode with Intel's game performance fixes.
:::

## Why it matters
At launch, Intel Core Ultra 200S (Arrow Lake) processors performed below expectations in many games. Intel found five causes. Most of them are fixed by a current BIOS together with Windows updates up to Windows 11 build 26100.2314 or newer. The last one needs a BIOS with microcode {{min}} and Intel CSME Firmware Kit 19.0.0.1854v2.2 or newer, for which Intel expected another improvement in the single-digit percent range on average over about 35 games [1]. Boards with older BIOS versions miss these fixes.

## How we detected it
We read the processor model and the microcode revision loaded by the BIOS. Model list and minimum revision come from the catalog.

## How to fix
1. Open the support page for your board ({{board}}).
2. **BitLocker:** if BitLocker or device encryption is on, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update or a change to the TPM or the boot configuration can make Windows ask for it at the next start [2][3].
3. Install the newest BIOS that lists microcode {{min}} and CSME firmware 19.0.0.1854v2.2 or newer, following the vendor's instructions [1].
4. Install Windows updates until Windows 11 reports build 26100.2314 or newer (Settings > System > About) [1].

## How to check the fix
Run the scan again. The microcode from the BIOS should be {{min}} or newer.

## Sources
1. https://community.intel.com/t5/Blogs/Tech-Innovation/Client/Field-Update-1-of-2-Intel-Core-Ultra-200S-Series-Performance/post/1650490
2. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
3. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
