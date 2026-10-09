using Optimizer.Core.Interop;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Hardware.Probes;

public static class FirmwareProbe
{
    public static FirmwareInfo Read()
    {
        var isUefi = Native.GetFirmwareType(out var type) && type == Native.FirmwareTypeUefi;

        var secureBoot = Reg.HklmInt(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State", "UEFISecureBootEnabled") switch
        {
            1 => TriState.Yes,
            0 => TriState.No,
            _ => isUefi ? TriState.Unknown : TriState.No,
        };

        TriState tpmPresent = TriState.Unknown, tpmReady = TriState.Unknown;
        string? tpmSpec = null;
        try
        {
            var tpm = Wmi.Query("SELECT IsEnabled_InitialValue, IsActivated_InitialValue, SpecVersion FROM Win32_Tpm", @"root\CIMV2\Security\MicrosoftTpm");
            if (tpm.Count == 0)
            {
                tpmPresent = TriState.No;
            }
            else
            {
                tpmPresent = TriState.Yes;
                tpmSpec = tpm[0].Str("SpecVersion").Split(',')[0].Trim();
                tpmReady = tpm[0].Bool("IsEnabled_InitialValue") == true && tpm[0].Bool("IsActivated_InitialValue") == true ? TriState.Yes : TriState.No;
            }
        }
        catch (Exception)
        {
            // Not elevated or TPM WMI provider missing -> Unknown.
        }

        int vbs = -1;
        bool hvci = false, credGuard = false, mbec = false, dma = false;
        try
        {
            var dg = Wmi.Query("SELECT * FROM Win32_DeviceGuard", @"root\Microsoft\Windows\DeviceGuard").FirstOrDefault();
            if (dg is not null)
            {
                vbs = dg.Int("VirtualizationBasedSecurityStatus") ?? -1;
                var running = dg.IntArray("SecurityServicesRunning");
                hvci = running.Contains(2);
                credGuard = running.Contains(1);
                var available = dg.IntArray("AvailableSecurityProperties");
                mbec = available.Contains(7);   // 7 = Mode Based Execution Control (Intel MBEC / AMD GMET)
                dma = available.Contains(3);    // 3 = DMA protection (IOMMU)
            }
        }
        catch (Exception)
        {
        }

        var bios = Wmi.Query("SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS").FirstOrDefault() ?? [];
        var board = Wmi.Query("SELECT Manufacturer, Product FROM Win32_BaseBoard").FirstOrDefault() ?? [];

        var style = PartitionStyle.Unknown;
        try
        {
            var disk = Wmi.Query("SELECT PartitionStyle, IsBoot, IsSystem FROM MSFT_Disk", @"root\Microsoft\Windows\Storage")
                .FirstOrDefault(d => d.Bool("IsSystem") == true || d.Bool("IsBoot") == true);
            style = disk?.Int("PartitionStyle") switch { 1 => PartitionStyle.Mbr, 2 => PartitionStyle.Gpt, _ => PartitionStyle.Unknown };
        }
        catch (Exception)
        {
        }

        return new FirmwareInfo(isUefi, secureBoot, tpmPresent, tpmSpec, tpmReady, vbs, hvci, credGuard, mbec, dma,
            bios.Str("Manufacturer"), bios.Str("SMBIOSBIOSVersion"), bios.Date("ReleaseDate"),
            board.Str("Manufacturer"), board.Str("Product"), style);
    }
}
