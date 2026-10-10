# Integrated graphics enabled but unused

## Summary
::: status Info
The integrated graphics ({{igpu}}) are enabled but no display uses them. That is fine; it does not cost frame rate.
:::
::: status Ok,Unknown,Problem,Unsupported
Checks whether the processor's integrated graphics are enabled on a desktop with a graphics card.
:::

## Why it matters
With a graphics card installed and no display on the integrated graphics, they are idle. They are useful for hardware video encoding (for example Intel Quick Sync in OBS or video editors) and for extra monitor outputs. When both GPUs are present, a game or program can pick the wrong one; Windows lets you choose the GPU per app [1].

## How we detected it
We check that both an integrated and a dedicated GPU are active and that no display is connected to the integrated one.

## How to fix
1. Keep it enabled if you use hardware video encoding or might need another display output.
2. If a game uses the wrong GPU, open **Settings > System > Display > Graphics**, select the game under **Custom options for apps**, select **Options**, choose **High performance** and select **Save** [1]. That is better than disabling the integrated graphics.
3. **BitLocker:** before you change BIOS settings, check whether BitLocker or device encryption is on. If so, suspend protection first (Start > **Manage BitLocker** > **Suspend protection**) or have the recovery key ready; it is often saved in your Microsoft account. A BIOS update or a change to the TPM or the boot configuration can make Windows ask for it at the next start [2][3].
4. Only if you do not need it at all: disable it in the BIOS (often "iGPU Multi-Monitor" or "Integrated Graphics" set to Disabled, with the primary display set to PCIe).

## How to check the fix
No check needed; this is information.

## Sources
1. https://support.microsoft.com/en-us/windows/hardware/display-graphics/optimizations-for-windowed-games-in-windows-11
2. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
3. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
