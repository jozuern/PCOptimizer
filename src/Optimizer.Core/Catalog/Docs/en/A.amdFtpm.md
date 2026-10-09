# AMD fTPM stutter (AM4)

## Summary
::: status Problem
This AM4 PC uses the AMD firmware TPM with a BIOS older than the fix for random stutter (AGESA 1.2.0.7). Update the BIOS.
:::
::: status Ok
The BIOS includes AMD's fix for fTPM related stutter.
:::
::: status Unknown,Info,Unsupported
Checks whether an AM4 PC with AMD fTPM has the BIOS fix for intermittent stutter.
:::

## Why it matters
On some AM4 Ryzen systems, the firmware TPM (fTPM) caused short system-wide stutters with audio dropouts, because it sometimes accessed the BIOS flash chip slowly [1]. AMD fixed this in AGESA 1.2.0.7, released in BIOS updates from May 2022 on. Since Windows 11 needs a TPM, almost every AM4 PC with Windows 11 has the fTPM on.

## How we detected it
We check that the processor is an AM4 Ryzen and that the active TPM comes from AMD (firmware TPM).
::: variant agesa
The AGESA version comes from the BIOS's own information tables.
:::
::: variant date
The BIOS does not report its AGESA version, so we use its date: releases from May 2022 on normally include the fix.
:::

## How to fix
1. Download the latest BIOS for your {{board}} from the manufacturer's support page and look for AGESA 1.2.0.7 or newer in the release notes.
::: if supportUrl
   Support page: {{supportUrl}}
:::
2. Update the BIOS as the manual describes. Do not turn the PC off during the update.
3. After the update, the BIOS settings are often reset: turn the fTPM, XMP/D.O.C.P and Secure Boot back on if needed.
4. If no fixed BIOS exists for your board, a separate TPM module (dTPM) avoids the problem.

## How to check the fix
Run the scan again. The AGESA version or BIOS date should be newer.

## Sources
1. https://www.amd.com/en/resources/support-articles/faqs/PA-410.html
