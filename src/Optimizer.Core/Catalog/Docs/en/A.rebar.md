# Resizable BAR

## Summary
::: variant off
{{gpu}} supports Resizable BAR, but it is off. Turn on Above 4G Decoding and Resizable BAR in the BIOS.
:::
::: variant laptop
Resizable BAR is off for {{gpu}}. In laptops, support depends on the model and comes with the maker's firmware; there is no setting to change.
:::
::: variant active
Resizable BAR is active for {{gpu}}: the processor can access all of the video memory at once.
:::
::: variant unsupported
{{gpu}} does not support Resizable BAR. Nothing to change. No BIOS setting can add it.
:::
::: variant unknownGpu,unknownState
Checks whether Resizable BAR (AMD: Smart Access Memory) is active for the graphics card.
:::

## Why it matters
Without Resizable BAR, the processor sees the video memory through a window that is usually 256 MB [3]. With it, Windows enlarges that window at startup so it covers the whole video memory [3]. NVIDIA enables it only in games where its tests show a gain, from a few percent up to 12 %, and some games run slower with it [1]. Intel says Arc cards need it for a good experience: without it, frame time spikes get larger, although the cards still work [4][5]. Support needs a recent graphics card (NVIDIA RTX 30 series and newer [1], AMD Radeon RX 5000 series and newer [7], Intel Arc), UEFI boot and a BIOS setting [6]. For Smart Access Memory, AMD names a Ryzen 3000 or newer processor on a 500 series board or newer [7].
::: variant unsupported
Older cards like the GeForce RTX 20 series do not support it, so the setting would have no effect.
:::
::: variant laptop
In laptops, Resizable BAR support depends on the model; only the laptop maker can add it with a firmware update [1][2].
:::

## How we detected it
We read the size of the graphics card's memory windows (PCI memory resources) through the Windows configuration manager. A window larger than 256 MB means Resizable BAR is active. Whether the card supports it comes from the catalog's GPU table. We also check UEFI boot and the system disk's partition style.

## How to fix
::: variant off
1. **BitLocker:** if BitLocker or device encryption is on, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update or a change to the TPM or the boot configuration can make Windows ask for it at the next start [9][10].
::: if mbr
2. Your system disk uses MBR. Turning off CSM (legacy boot) would make Windows unbootable. In a command prompt run as administrator, check the disk with `mbr2gpt /validate /allowFullOS` and convert it with `mbr2gpt /convert /allowFullOS`, then switch the firmware to UEFI [8]. Back up first; BitLocker protection must be suspended for the conversion [8].
:::
3. Update the BIOS to a version with Resizable BAR support. If the board maker offers no such version, the platform does not support it.
4. GeForce RTX 3060 Ti, 3070, 3080 and 3090 cards may need a graphics card firmware (VBIOS) update first [1]. NVIDIA's update tool page and the card maker's support page have it [2].
::: if menuPath
5. On your {{board}}: **{{menuPath}}**.
::: if menuUnverified
   This path is not yet checked against the manual for your board. Menu names differ between boards and BIOS versions, so search for the setting by name if the path does not match.
:::
:::
::: ifnot menuPath
5. Enable **Above 4G Decoding** and **Re-Size BAR Support**. Some boards call it Smart Access Memory or Clever Access Memory (ASRock: C.A.M.) [6].
:::
6. Make sure **CSM** is disabled (pure UEFI boot) and the Windows disk uses GPT [2][6].
7. Update the graphics driver.
:::
::: variant laptop,active,unsupported,unknownGpu,unknownState
No action needed.
:::

## How to check the fix
Run the scan again. The largest memory window should match the video memory size. NVIDIA Control Panel > System Information also shows "Resizable BAR: Yes" [2].

## Sources
1. https://www.nvidia.com/en-us/geforce/news/geforce-rtx-30-series-resizable-bar-support/
2. https://nvidia.custhelp.com/app/answers/detail/a_id/5165/~/nvidia-resizable-bar-firmware-update-tool
3. https://learn.microsoft.com/en-us/windows-hardware/drivers/display/resizable-bar-support
4. https://game.intel.com/stories/intel-arc-graphics-resizable-bar/
5. https://www.intel.com/content/www/us/en/support/articles/000092416/graphics.html
6. https://www.intel.com/content/www/us/en/support/articles/000090831/graphics.html
7. https://www.amd.com/en/legal/claims/gaming-details.html
8. https://learn.microsoft.com/en-us/windows/deployment/mbr-to-gpt
9. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
10. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
