# BIOS older than a year

## Summary
::: status Info
The BIOS of {{board}} is more than a year old. Check whether a newer version exists; updates often fix stability and compatibility issues.
:::
::: status Ok,Unknown,Problem,Unsupported
Checks the age of the BIOS.
:::

## Why it matters
BIOS updates carry new processor microcode, compatibility fixes and security fixes. Intel and AMD, for example, deliver fixes for processor stability and for system pauses through BIOS updates [1][2]. Some updates add support for features such as Smart Access Memory (Resizable BAR) [3]. Not every update matters for gaming, and if the manufacturer stopped releasing updates for an older board, there is simply nothing newer. That is why this is information, not a problem.

## How we detected it
We read the BIOS release date and version from Windows.

## How to fix
::: ifnot laptop
1. Open the support page of your {{board}} and compare the newest BIOS version with yours.
::: if supportUrl
   Support page: {{supportUrl}}
:::
2. Read the release notes. Update if they fix something relevant for you, using the method in the manual (often a USB stick and the BIOS's flash tool).
3. **BitLocker:** if BitLocker or device encryption is on, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update or a change to the TPM or the boot configuration can make Windows ask for it at the next start [4][5].
4. Do not turn the PC off during the update. The BIOS settings may be reset afterwards: turn XMP or EXPO, Secure Boot and TPM back on if needed.
:::
::: if laptop
1. Laptop BIOS updates come from the manufacturer's support app or website, sometimes also through Windows Update. Plug in the charger before updating.
2. **BitLocker:** if BitLocker or device encryption is on, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update can make Windows ask for it at the next start [4][5].
:::

## How to check the fix
Run the scan again. The BIOS date should be newer.

## Sources
1. https://www.intel.com/content/www/us/en/support/articles/000102331/processors.html
2. https://www.amd.com/en/resources/support-articles/faqs/PA-410.html
3. https://www.amd.com/en/legal/claims/gaming-details.html
4. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
5. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
