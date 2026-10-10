using System.Runtime.InteropServices;
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

        // The WMI queries go to different namespaces and each one costs a connection: they run side by side.
        var tpm = Task.Run(ReadTpm);
        var deviceGuard = Task.Run(ReadDeviceGuard);
        var bios = Task.Run(() => Row("SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS"));
        var board = Task.Run(() => Row("SELECT Manufacturer, Product FROM Win32_BaseBoard"));
        var style = Task.Run(ReadSystemDiskStyle);
        Task.WaitAll(tpm, deviceGuard, bios, board, style);

        var (tpmPresent, tpmSpec, tpmReady, tpmMaker) = tpm.Result;
        var (vbs, hvci, credGuard, mbec, dma) = deviceGuard.Result;
        return new FirmwareInfo(isUefi, secureBoot, tpmPresent, tpmSpec, tpmReady, vbs, hvci, credGuard, mbec, dma,
            bios.Result.Str("Manufacturer"), bios.Result.Str("SMBIOSBIOSVersion"), bios.Result.Date("ReleaseDate"),
            board.Result.Str("Manufacturer"), board.Result.Str("Product"), style.Result)
        {
            TpmManufacturer = tpmMaker,
        };
    }

    /// <summary>
    /// Win32_Tpm needs administrator rights; without them it only answers "access denied", after several seconds. Not
    /// elevated, the TPM Base Services (Tbsi_GetDeviceInfo, allowed for every user) give presence and version instead.
    /// </summary>
    private static (TriState Present, string? Spec, TriState Ready, string? Manufacturer) ReadTpm()
    {
        if (!DataPaths.ProcessIsElevated) return ReadTpmFromTbs();
        try
        {
            var tpm = Wmi.Query("SELECT IsEnabled_InitialValue, IsActivated_InitialValue, SpecVersion, ManufacturerIdTxt FROM Win32_Tpm", @"root\CIMV2\Security\MicrosoftTpm");
            if (tpm.Count == 0) return (TriState.No, null, TriState.Unknown, null);
            var ready = tpm[0].Bool("IsEnabled_InitialValue") == true && tpm[0].Bool("IsActivated_InitialValue") == true ? TriState.Yes : TriState.No;
            return (TriState.Yes, tpm[0].Str("SpecVersion").Split(',')[0].Trim(), ready, tpm[0].Str("ManufacturerIdTxt") is { Length: > 0 } m ? m : null);
        }
        catch (Exception)
        {
            // TPM WMI provider missing: what TBS knows.
            return ReadTpmFromTbs();
        }
    }

    private static (TriState, string?, TriState, string?) ReadTpmFromTbs()
    {
        try
        {
            var info = new TpmDeviceInfo { StructVersion = 1 };
            var result = Tbsi_GetDeviceInfo((uint)Marshal.SizeOf<TpmDeviceInfo>(), ref info);
            if (result == TbsETpmNotFound) return (TriState.No, null, TriState.Unknown, null);
            if (result != 0) return (TriState.Unknown, null, TriState.Unknown, null);
            return (TriState.Yes, info.TpmVersion switch { 2 => "2.0", 1 => "1.2", _ => null }, TriState.Unknown, null);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return (TriState.Unknown, null, TriState.Unknown, null);
        }
    }

    private static (int Vbs, bool Hvci, bool CredentialGuard, bool Mbec, bool Dma) ReadDeviceGuard()
    {
        try
        {
            var dg = Wmi.Query("SELECT * FROM Win32_DeviceGuard", @"root\Microsoft\Windows\DeviceGuard").FirstOrDefault();
            if (dg is null) return (-1, false, false, false, false);
            var running = dg.IntArray("SecurityServicesRunning");
            var available = dg.IntArray("AvailableSecurityProperties");
            // Running: 1 = Credential Guard, 2 = memory integrity (HVCI). Available: 3 = DMA protection (IOMMU),
            // 7 = Mode Based Execution Control (Intel MBEC / AMD GMET).
            return (dg.Int("VirtualizationBasedSecurityStatus") ?? -1, running.Contains(2), running.Contains(1), available.Contains(7), available.Contains(3));
        }
        catch (Exception)
        {
            return (-1, false, false, false, false);
        }
    }

    private static PartitionStyle ReadSystemDiskStyle()
    {
        try
        {
            var disk = Wmi.Query("SELECT PartitionStyle, IsBoot, IsSystem FROM MSFT_Disk", @"root\Microsoft\Windows\Storage")
                .FirstOrDefault(d => d.Bool("IsSystem") == true || d.Bool("IsBoot") == true);
            return disk?.Int("PartitionStyle") switch { 1 => PartitionStyle.Mbr, 2 => PartitionStyle.Gpt, _ => PartitionStyle.Unknown };
        }
        catch (Exception)
        {
            return PartitionStyle.Unknown;
        }
    }

    // Each query on its own: a failing BIOS or board query leaves those fields empty instead of losing the whole section.
    private static Dictionary<string, object?> Row(string wql)
    {
        try
        {
            return Wmi.Query(wql).FirstOrDefault() ?? [];
        }
        catch (Exception ex)
        {
            Logging.Log.Warn("scan", $"firmware: {wql}: {ex.Message}");
            return [];
        }
    }

    private const uint TbsETpmNotFound = 0x8028400F;

    /// <summary>TPM_DEVICE_INFO (tbs.h): structVersion 1; tpmVersion 1 = TPM 1.2, 2 = TPM 2.0.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct TpmDeviceInfo
    {
        public uint StructVersion;
        public uint TpmVersion;
        public uint TpmInterfaceType;
        public uint TpmImpRevision;
    }

    [DllImport("tbs.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint Tbsi_GetDeviceInfo(uint size, ref TpmDeviceInfo info);
}
