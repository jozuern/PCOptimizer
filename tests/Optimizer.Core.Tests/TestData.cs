using Optimizer.Core.Hardware;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Tests;

/// <summary>Mocked hardware pieces for rule tests.</summary>
internal static class TestData
{
    public static BuildInfo Os(int build = 26300) => new(build, 9550, "26H2", "Professional", "Windows 11 Pro", CpuArchitecture.X64, false, null);

    public static CpuInfo Cpu9700K() => new("Intel(R) Core(TM) i7-9700K CPU @ 3.60GHz", Vendor.Intel, 6, 158, 12, 8, 8, 3600, "LGA1151",
        0xF8, 0xF8, "test", [new CacheDomain(3, 12L << 20, 0xFF, 0)], new Dictionary<int, int> { [0] = 8 });

    public static FirmwareInfo Firmware(string board = "ASUSTeK COMPUTER INC.") => new(true, TriState.Yes, TriState.Yes, "2.0", TriState.Yes,
        2, false, false, true, true, "American Megatrends Inc.", "1802", new DateTime(2023, 3, 1), board, "ROG STRIX Z390-F GAMING", PartitionStyle.Gpt);

    public static DisplayInfo Display(string name, int currentHz, int maxHz) => new($@"\\.\DISPLAY{name.Length}", name, "", false, 10,
        2560, 1440, new RefreshRate((uint)currentHz * 1000, 1000), maxHz, [], "", Vendor.Nvidia, "NVIDIA GeForce RTX 2070", false, false, null);

    public static GpuInfo Gpu(string name, Vendor vendor, long? bar) => new(name, vendor, $@"PCI\VEN_10DE&DEV_0000\{name.Length}", "32.0.16.1742",
        DateTime.Today, "oem1.inf", "NVIDIA", 8L << 30, GpuKind.Discrete, bar, null, null, 0, TriState.Unknown);
}
