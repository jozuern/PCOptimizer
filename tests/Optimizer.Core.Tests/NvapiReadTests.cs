using Optimizer.Core.Interop;
using Xunit.Abstractions;

namespace Optimizer.Core.Tests;

/// <summary>Read-only NVAPI DRS checks on this machine (Category=Hardware). No setting is written or saved.</summary>
public class NvapiReadTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Hardware")]
    public void BaseProfileSettingsAreReadable()
    {
        if (!Nvapi.Available)
        {
            output.WriteLine("NVAPI not available on this machine");
            return;
        }
        using var s = new Nvapi.Session();
        var profile = s.BaseProfile();
        foreach (var (name, id) in new[]
                 {
                     ("PowerMode", Nvapi.SettingPreferredPState), ("FRL", Nvapi.SettingFrameRateLimiter), ("VSync", Nvapi.SettingVSyncMode),
                     ("PreRender", Nvapi.SettingPreRenderLimit), ("ShaderCache", Nvapi.SettingShaderCacheMaxSize), ("Texture", Nvapi.SettingTextureQuality),
                     ("BatteryBoost", Nvapi.SettingBatteryBoostFps), ("GsyncMode", Nvapi.SettingVrrMode),
                 })
        {
            var status = s.GetStatus(profile, id, out var value, out var location);
            output.WriteLine($"{name,-12} status={status} value=0x{value:X} location={location}");
            // 0 = set somewhere, -160 = not set (driver default). -9 would mean our struct layout is wrong.
            Assert.True(status is 0 or -160, $"{name}: NVAPI status {status}");
        }
    }
}
