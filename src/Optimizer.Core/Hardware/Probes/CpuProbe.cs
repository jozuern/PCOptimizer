using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Optimizer.Core.Interop;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Hardware.Probes;

public static partial class CpuProbe
{
    private const string CpuKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

    public static CpuInfo Read()
    {
        var row = Wmi.Query("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, SocketDesignation FROM Win32_Processor").FirstOrDefault()
                  ?? new Dictionary<string, object?>();
        var name = Reg.HklmString(CpuKey, "ProcessorNameString")?.Trim() ?? row.Str("Name");
        var vendorId = Reg.HklmString(CpuKey, "VendorIdentifier") ?? row.Str("Manufacturer");
        var vendor = vendorId.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? Vendor.Intel
                   : vendorId.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? Vendor.Amd
                   : vendorId.Contains("Qualcomm", StringComparison.OrdinalIgnoreCase) ? Vendor.Qualcomm : Vendor.Other;
        var (family, model, stepping) = ParseIdentifier(Reg.HklmString(CpuKey, "Identifier"));
        var (current, bios, source) = ReadMicrocode(vendor);
        var (l3, effClasses, coreCount) = ReadTopology();
        var sockets = Wmi.Query("SELECT NumberOfCores FROM Win32_Processor").Sum(r => r.Int("NumberOfCores") ?? 0);

        return new CpuInfo(
            name,
            vendor,
            family,
            model,
            stepping,
            Math.Max(sockets, coreCount),
            row.Int("NumberOfLogicalProcessors") ?? Environment.ProcessorCount,
            row.Int("MaxClockSpeed") ?? 0,
            row.Str("SocketDesignation"),
            current,
            bios,
            source,
            l3,
            effClasses);
    }

    /// <summary>"Intel64 Family 6 Model 158 Stepping 12" / "AMD64 Family 25 Model 97 Stepping 2".</summary>
    public static (int Family, int Model, int Stepping) ParseIdentifier(string? identifier)
    {
        if (identifier is null) return (0, 0, 0);
        var m = IdentifierRegex().Match(identifier);
        return m.Success ? (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value)) : (0, 0, 0);
    }

    [GeneratedRegex(@"Family\s+(\d+)\s+Model\s+(\d+)\s+Stepping\s+(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex IdentifierRegex();

    /// <summary>
    /// Microcode revision from an "Update Revision"-style REG_BINARY.
    /// Observed on 26300 (Gaming PC, i7-9700K): 4 bytes, little-endian revision (F8 00 00 00 -> 0xF8).
    /// Older builds store the 8-byte MSR 0x8B copy: Intel revision in bytes 4-7, AMD patch level in bytes 0-3.
    /// </summary>
    public static uint? ParseMicrocodeBinary(byte[]? value, Vendor vendor)
    {
        if (value is null) return null;
        return value.Length switch
        {
            4 => BitConverter.ToUInt32(value, 0),
            8 => vendor == Vendor.Amd ? BitConverter.ToUInt32(value, 0) : BitConverter.ToUInt32(value, 4),
            _ => null,
        };
    }

    private static (uint? Current, uint? Bios, string Source) ReadMicrocode(Vendor vendor)
    {
        var current = ParseMicrocodeBinary(Reg.HklmValue(CpuKey, "Update Revision") as byte[], vendor);
        // "Firmware Record Version" (DWORD) = revision loaded by the BIOS; present on current builds (confirmed on 26300).
        if (Reg.HklmValue(CpuKey, "Firmware Record Version") is int firmware && firmware != 0)
            return (current, (uint)firmware, "Firmware Record Version");
        // Fallback: the revision before the OS-loaded update.
        var previous = ParseMicrocodeBinary(Reg.HklmValue(CpuKey, "Previous Update Revision") as byte[], vendor);
        if (previous is > 0) return (current, previous, "Previous Update Revision");
        return (current, null, "unavailable");
    }

    private static (List<CacheDomain> L3, Dictionary<int, int> EfficiencyClasses, int Cores) ReadTopology()
    {
        var l3 = new List<CacheDomain>();
        var classes = new Dictionary<int, int>();
        var cores = 0;
        uint len = 0;
        Native.GetLogicalProcessorInformationEx(Native.RelationAll, IntPtr.Zero, ref len);
        if (len == 0) return (l3, classes, 0);
        var buffer = Marshal.AllocHGlobal((int)len);
        try
        {
            if (!Native.GetLogicalProcessorInformationEx(Native.RelationAll, buffer, ref len)) return (l3, classes, 0);
            var data = new byte[len];
            Marshal.Copy(buffer, data, 0, (int)len);
            ParseTopology(data, l3, classes, ref cores);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        return (l3, classes, cores);
    }

    /// <summary>Parses SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX records (Relationship: 4 bytes, Size: 4 bytes, union from offset 8).</summary>
    internal static void ParseTopology(byte[] data, List<CacheDomain> l3, Dictionary<int, int> classes, ref int cores)
    {
        var offset = 0;
        while (offset + 8 <= data.Length)
        {
            var relation = BitConverter.ToInt32(data, offset);
            var size = BitConverter.ToInt32(data, offset + 4);
            if (size <= 0) break;
            var u = offset + 8;
            if (relation == Native.RelationProcessorCore)
            {
                // PROCESSOR_RELATIONSHIP: BYTE Flags; BYTE EfficiencyClass; ...
                int eff = data[u + 1];
                classes[eff] = classes.GetValueOrDefault(eff) + 1;
                cores++;
            }
            else if (relation == Native.RelationCache)
            {
                // CACHE_RELATIONSHIP: Level(1) Assoc(1) LineSize(2) CacheSize(4) Type(4) Reserved(18) GroupCount(2) GroupMask(GROUP_AFFINITY: Mask 8, Group 2, ...)
                var level = data[u];
                var cacheSize = BitConverter.ToUInt32(data, u + 4);
                var type = BitConverter.ToInt32(data, u + 8);
                var mask = BitConverter.ToUInt64(data, u + 32);
                var group = BitConverter.ToUInt16(data, u + 40);
                if (level == 3 && type is 0) l3.Add(new CacheDomain(level, cacheSize, mask, group));
            }
            offset += size;
        }
    }
}
