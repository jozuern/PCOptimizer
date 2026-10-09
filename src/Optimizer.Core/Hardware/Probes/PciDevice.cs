using Optimizer.Core.Interop;

namespace Optimizer.Core.Hardware.Probes;

/// <summary>CfgMgr32 helpers: device properties, parents, memory resources. Read-only.</summary>
public static class PciDevice
{
    private static readonly Guid PciFmtId = new("3ab22e31-8264-4b4e-9af5-a8d2d8e33e62");

    // DEVPKEY_PciDevice_* property ids (devpkey.h). Confirmed on real hardware in M1 (see docs/hardware-notes.md).
    private const uint PidBaseClass = 3, PidSubClass = 4, PidCurrentLinkSpeed = 9, PidCurrentLinkWidth = 10, PidMaxLinkSpeed = 11, PidMaxLinkWidth = 12,
        PidInterruptSupport = 14, PidInterruptMessageMaximum = 15;

    public static uint? Locate(string instanceId) =>
        Native.CM_Locate_DevNodeW(out var dev, instanceId, 0) == Native.CrSuccess ? dev : null;

    public static uint? Parent(uint devInst) =>
        Native.CM_Get_Parent(out var parent, devInst, 0) == Native.CrSuccess ? parent : null;

    public static string? InstanceId(uint devInst)
    {
        var buffer = new char[512];
        return Native.CM_Get_Device_IDW(devInst, buffer, buffer.Length, 0) == Native.CrSuccess
            ? new string(buffer).TrimEnd('\0')
            : null;
    }

    public static uint? PciUInt32(uint devInst, uint pid) => ReadUInt32(devInst, PciFmtId, pid);

    private static uint? ReadUInt32(uint devInst, Guid fmtid, uint pid)
    {
        var key = new Native.DEVPROPKEY { fmtid = fmtid, pid = pid };
        uint size = 4;
        var buffer = new byte[4];
        var cr = Native.CM_Get_DevNode_PropertyW(devInst, ref key, out var type, buffer, ref size, 0);
        if (cr != Native.CrSuccess || size < 1) return null;
        // DEVPROP_TYPE_UINT32 = 7, BYTE = 3, UINT16 = 5
        return type switch
        {
            7 => BitConverter.ToUInt32(buffer, 0),
            5 => BitConverter.ToUInt16(buffer, 0),
            3 => buffer[0],
            _ => null,
        };
    }

    public static PcieLink? Link(uint devInst)
    {
        var cs = PciUInt32(devInst, PidCurrentLinkSpeed);
        var cw = PciUInt32(devInst, PidCurrentLinkWidth);
        var ms = PciUInt32(devInst, PidMaxLinkSpeed);
        var mw = PciUInt32(devInst, PidMaxLinkWidth);
        if (cs is null && cw is null && ms is null && mw is null) return null;
        return new PcieLink(PcieLink.SpeedCodeToGen(cs), (int?)cw, PcieLink.SpeedCodeToGen(ms), (int?)mw);
    }

    /// <summary>
    /// DEVPKEY_PciDevice_InterruptSupport (pid 14): bit 0 line-based, bit 1 MSI, bit 2 MSI-X. Null when unknown.
    /// </summary>
    public static uint? InterruptSupport(uint devInst) => PciUInt32(devInst, PidInterruptSupport);

    /// <summary>DEVPKEY_PciDevice_InterruptMessageMaximum (pid 15): MSI messages the device can use.</summary>
    public static uint? InterruptMessageMaximum(uint devInst) => PciUInt32(devInst, PidInterruptMessageMaximum);

    /// <summary>
    /// Disables and re-enables a device so its driver re-reads the registry (what Device Manager does for advanced
    /// properties). Returns null on success, otherwise a reason. A disabled device is always re-enabled.
    /// </summary>
    public static string? Restart(string instanceId)
    {
        if (Locate(instanceId) is not { } dev) return $"device {instanceId} not found";
        var cr = Interop.NativeWrite.CM_Disable_DevNode(dev, Interop.NativeWrite.CmDisableUiNotOk);
        if (cr != Native.CrSuccess) return $"CM_Disable_DevNode returned {cr}";
        cr = Interop.NativeWrite.CM_Enable_DevNode(dev, 0);
        return cr == Native.CrSuccess ? null : $"CM_Enable_DevNode returned {cr}";
    }

    public static bool IsPciBridge(uint devInst) => PciUInt32(devInst, PidBaseClass) == 6 && PciUInt32(devInst, PidSubClass) == 4;

    public static string? VendorId(string? instanceId)
    {
        if (instanceId is null) return null;
        var i = instanceId.IndexOf("VEN_", StringComparison.OrdinalIgnoreCase);
        return i >= 0 && instanceId.Length >= i + 8 ? instanceId.Substring(i + 4, 4).ToUpperInvariant() : null;
    }

    /// <summary>
    /// PCIe walk (plan v4 §5.3): Radeon RX 5000+ (and possibly Intel Arc) have a PCIe switch on the card, so the GPU's
    /// direct parent is that switch. Walk up past bridge devices with the GPU's vendor ID that are not root ports;
    /// the node we stop at is the card's upstream port and its parent is the first platform port (the slot).
    /// </summary>
    public static (PcieLink? Card, PcieLink? Platform, int Hops) WalkGpuLink(uint gpuDevInst, IReadOnlySet<string> switchVendors)
    {
        var gpuVendor = VendorId(InstanceId(gpuDevInst));
        var node = gpuDevInst;
        var hops = 0;
        if (gpuVendor is not null && switchVendors.Contains(gpuVendor))
        {
            while (hops < 4 && Parent(node) is { } parent)
            {
                var parentId = InstanceId(parent);
                var grandParentId = Parent(parent) is { } gp ? InstanceId(gp) : null;
                var parentIsRootPort = grandParentId is null || !grandParentId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase);
                if (parentId is null || !parentId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) || parentIsRootPort) break;
                if (VendorId(parentId) != gpuVendor || !IsPciBridge(parent)) break;
                node = parent;
                hops++;
            }
        }
        var card = Link(node);
        // The card's own capability is the GPU endpoint's maximum (the on-card switch may report its own).
        var gpuOwn = hops > 0 ? Link(gpuDevInst) : card;
        if (card is not null && gpuOwn is not null && hops > 0)
            card = card with { MaxGen = card.MaxGen ?? gpuOwn.MaxGen, MaxWidth = card.MaxWidth ?? gpuOwn.MaxWidth };
        PcieLink? platform = null;
        if (Parent(node) is { } port && InstanceId(port) is { } portId && portId.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase))
            platform = Link(port);
        return (card, platform, hops);
    }

    /// <summary>Largest memory BAR in the allocated resource list (ReBAR active -> a BAR in the size of the VRAM, not 256 MB).</summary>
    public static long? LargestMemoryRange(uint devInst)
    {
        if (Native.CM_Get_First_Log_Conf(out var logConf, devInst, Native.AllocLogConf) != Native.CrSuccess) return null;
        long largest = 0;
        try
        {
            var current = logConf;
            while (Native.CM_Get_Next_Res_Des(out var resDes, current, Native.ResTypeAll, out var resType, 0) == Native.CrSuccess)
            {
                if (current != logConf) Native.CM_Free_Res_Des_Handle(current);
                current = resDes;
                if (resType is not (Native.ResTypeMem or Native.ResTypeMemLarge)) continue;
                if (Native.CM_Get_Res_Des_Data_Size(out var size, resDes, 0) != Native.CrSuccess || size < 24) continue;
                var data = new byte[size];
                if (Native.CM_Get_Res_Des_Data(resDes, data, size, 0) != Native.CrSuccess) continue;
                // MEM_DES / MEM_LARGE_DES: Count(4) Type(4) Alloc_Base(8) Alloc_End(8) ...
                var start = BitConverter.ToUInt64(data, 8);
                var end = BitConverter.ToUInt64(data, 16);
                if (end > start) largest = Math.Max(largest, (long)(end - start + 1));
            }
            if (current != logConf) Native.CM_Free_Res_Des_Handle(current);
        }
        finally
        {
            Native.CM_Free_Log_Conf_Handle(logConf);
        }
        return largest == 0 ? null : largest;
    }
}
