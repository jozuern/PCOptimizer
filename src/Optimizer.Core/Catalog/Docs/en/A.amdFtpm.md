# AMD fTPM stutter (AM4)

## Summary
::: status Problem
This AM4 desktop uses the AMD firmware TPM with a BIOS older than AMD's fix for short system pauses (AGESA 1.2.0.7). Update the BIOS.
:::
::: status Ok
The BIOS includes AMD's fix for pauses caused by the firmware TPM.
:::
::: status Unknown,Info,Unsupported
Checks whether an AM4 desktop with AMD fTPM has the BIOS fix for intermittent system pauses.
:::

## Why it matters
AMD found that on select Ryzen systems the firmware TPM (fTPM) can sometimes take long to access the flash chip on the mainboard, which causes short pauses in which the whole system stops responding [1]. AMD's fix is in BIOS releases based on AGESA 1.2.0.7 or newer, available from early May 2022 depending on the board maker [1]. Windows 11 requires TPM 2.0 [2], so on AM4 PCs with Windows 11 the fTPM is usually on.

## How we detected it
We check that the PC is a desktop with an AM4 Ryzen processor and that the active TPM comes from AMD (firmware TPM). Laptops and mini PCs with mobile Ryzen processors are not checked, because AMD's AGESA version applies to the desktop AM4 platform.
::: variant agesa
The AGESA version comes from the BIOS's own information tables.
:::
::: variant date
The BIOS does not report a desktop AM4 AGESA version, so we use its date: releases from May 2022 on normally include the fix.
:::

## How to fix
1. Download the latest BIOS for your {{board}} from the manufacturer's support page and look for AGESA 1.2.0.7 or newer in the release notes [1].
::: if supportUrl
   Support page: {{supportUrl}}
:::
2. **BitLocker:** if BitLocker or device encryption is on, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update or a change to the TPM or the boot configuration can make Windows ask for it at the next start [3][4].
3. Update the BIOS as the manual describes. Do not turn the PC off during the update.
4. After the update, the BIOS settings are often reset: turn the fTPM, the memory profile (XMP or DOCP) and Secure Boot back on if needed.
5. If no fixed BIOS exists for your board, AMD names a separate TPM module (dTPM) as the workaround. Check that the board supports one, and turn off BitLocker or device encryption or back up your data before you switch the TPM [1].

## How to check the fix
Run the scan again. The AGESA version or BIOS date should be newer.

## Sources
1. https://www.amd.com/en/resources/support-articles/faqs/PA-410.html
2. https://support.microsoft.com/en-us/windows/windows-11-system-requirements-86c11283-ea52-4782-9efd-7674389a7ba3
3. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
4. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
