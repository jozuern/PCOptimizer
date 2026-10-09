# BIOS older than a year

## Summary
::: status Info
The BIOS of {{board}} is more than a year old. Check whether a newer version exists; updates often fix stability and compatibility issues.
:::
::: status Ok,Unknown,Problem,Unsupported
Checks the age of the BIOS.
:::

## Why it matters
BIOS updates carry new processor microcode, memory compatibility fixes, security fixes and sometimes new features such as Resizable BAR. Not every update matters for gaming, and if the manufacturer stopped releasing updates for an older board, there is simply nothing newer. That is why this is information, not a problem.

## How we detected it
We read the BIOS release date and version from Windows.

## How to fix
::: ifnot laptop
1. Open the support page of your {{board}} and compare the newest BIOS version with yours.
::: if supportUrl
   Support page: {{supportUrl}}
:::
2. Read the release notes. Update if they fix something relevant for you, using the method in the manual (often a USB stick and the BIOS's flash tool).
3. Do not turn the PC off during the update. The BIOS settings may be reset afterwards: turn XMP/EXPO, Secure Boot and TPM back on if needed.
:::
::: if laptop
1. Laptop BIOS updates come from the manufacturer's support app or website, sometimes also through Windows Update. Plug in the charger before updating.
:::

## How to check the fix
Run the scan again. The BIOS date should be newer.
