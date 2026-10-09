using System.Runtime.InteropServices;
using System.Text;

namespace Optimizer.Core.Interop;

/// <summary>
/// Minimal NVAPI interop (nvapi64.dll, MIT-licensed SDK). Function IDs come from nvapi_interface.h, struct layouts
/// from nvapi.h (pack 8), setting IDs from NvApiDriverSettings.h. Only documented DRS settings are used.
/// </summary>
public static class Nvapi
{
    public const int Ok = 0;

    // nvapi_interface.h
    private const uint IdInitialize = 0x0150e828;
    private const uint IdDrsCreateSession = 0x0694d52e;
    private const uint IdDrsDestroySession = 0xdad9cff8;
    private const uint IdDrsLoadSettings = 0x375dbd6b;
    private const uint IdDrsSaveSettings = 0xfcbc7e14;
    private const uint IdDrsGetBaseProfile = 0xda8466a0;
    private const uint IdDrsGetSetting = 0x73bf8338;
    private const uint IdDrsSetSetting = 0x577dd202;
    private const uint IdDrsDeleteProfileSetting = 0xe4a26362;
    private const uint IdDrsFindApplicationByName = 0xeee566b2;
    private const uint IdDrsFindProfileByName = 0x7e4a9a0b;
    private const uint IdDrsCreateProfile = 0xcc176068;
    private const uint IdDrsCreateApplication = 0x4347a9de;
    private const uint IdSysGetDriverAndBranchVersion = 0x2926aaad;
    private const uint IdDispGetDisplayIdByDisplayName = 0xae457190;
    private const uint IdDispGetVrrInfo = 0xdf8fda57;

    // NvApiDriverSettings.h
    public const uint SettingPreferredPState = 0x1057EB71;   // Power management mode
    public const uint SettingFrameRateLimiter = 0x10835002;  // FRL_FPS: 0 = off, else FPS
    public const uint SettingVSyncMode = 0x00A879CF;
    public const uint SettingPreRenderLimit = 0x007BA09E;    // Maximum pre-rendered frames (Low Latency Mode On = 1)
    public const uint SettingShaderCacheMaxSize = 0x00AC8497;
    public const uint SettingTextureQuality = 0x00CE2691;    // QUALITY_ENHANCEMENTS
    public const uint SettingBatteryBoostFps = 0x10115C8C;
    public const uint SettingVrrMode = 0x1194F158;           // "Enable G-SYNC globally"

    public const uint VSyncForceOn = 0x47814940;
    public const uint VSyncForceOff = 0x08416747;
    public const uint VSyncPassive = 0x60925292;
    public const uint PStatePreferMax = 1;
    public const uint PStatePreferMin = 4;
    public const uint PStateOptimalPower = 5;
    public const uint TextureHighPerformance = 0x14;
    public const uint ShaderCacheUnlimited = 0xFFFFFFFF;

    // Struct sizes (pack 8): NvAPI_UnicodeString = NvU16[2048] = 4096 bytes.
    private const int UnicodeStringBytes = 4096;
    // Sizes verified with ctypes (MSVC x64 alignment). NVDRS_SETTING_V1 exists in two sizes: SDKs since the QWORD
    // union member use 12328 (current value at 8224); drivers built against the older layout expect 12320 (8220).
    // The session tries the current layout first and falls back on NVAPI_INCOMPATIBLE_STRUCT_VERSION (-9).
    private static readonly (int Size, int CurrentOffset)[] SettingLayouts = [(12328, 8224), (12320, 8220)];
    public const int ApplicationSize = 20492;       // NVDRS_APPLICATION_V4
    public const int ProfileSize = 4116;            // NVDRS_PROFILE_V1
    private const int IncompatibleStructVersion = -9;
    private const uint ApplicationVersion = ApplicationSize | (4u << 16);
    private const uint ProfileVersion = ProfileSize | (1u << 16);
    private const int VrrInfoSize = 24;
    private const uint VrrInfoVersion = VrrInfoSize | (1u << 16);

    // NVDRS_SETTING_V1 offsets
    private const int OffSettingId = 4 + UnicodeStringBytes;   // 4100
    private const int OffSettingType = OffSettingId + 4;      // 4104
    private const int OffSettingLocation = OffSettingType + 4; // 4108

    [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr QueryInterface(uint id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int NoArg();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int OutHandle(out IntPtr handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Handle1(IntPtr session);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int HandleOut(IntPtr session, out IntPtr profile);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int GetSettingFn(IntPtr session, IntPtr profile, uint id, byte[] setting);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int SetSettingFn(IntPtr session, IntPtr profile, byte[] setting);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int DeleteSettingFn(IntPtr session, IntPtr profile, uint id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FindAppFn(IntPtr session, byte[] appName, out IntPtr profile, byte[] app);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int FindProfileFn(IntPtr session, byte[] name, out IntPtr profile);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int CreateProfileFn(IntPtr session, byte[] profileInfo, out IntPtr profile);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int CreateAppFn(IntPtr session, IntPtr profile, byte[] app);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int DriverVersionFn(out uint version, byte[] branch);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int DisplayIdFn([MarshalAs(UnmanagedType.LPStr)] string name, out uint displayId);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int VrrInfoFn(uint displayId, byte[] info);

    private static readonly Lock Gate = new();
    private static bool? _available;

    /// <summary>Index into SettingLayouts that the installed driver accepted last.</summary>
    private static int PreferredLayout;

    private static T Fn<T>(uint id) where T : Delegate
    {
        var p = QueryInterface(id);
        if (p == IntPtr.Zero) throw new EntryPointNotFoundException($"NVAPI function 0x{id:X8} not available");
        return Marshal.GetDelegateForFunctionPointer<T>(p);
    }

    /// <summary>True when nvapi64.dll is present and NvAPI_Initialize succeeds (NVIDIA driver installed).</summary>
    public static bool Available
    {
        get
        {
            lock (Gate)
            {
                if (_available is { } a) return a;
                try
                {
                    _available = File.Exists(Path.Combine(Environment.SystemDirectory, "nvapi64.dll")) && Fn<NoArg>(IdInitialize)() == Ok;
                }
                catch (Exception)
                {
                    _available = false;
                }
                return _available.Value;
            }
        }
    }

    /// <summary>Driver version as NVIDIA shows it, e.g. "617.42".</summary>
    public static string? DriverVersion()
    {
        if (!Available) return null;
        var branch = new byte[64];
        return Fn<DriverVersionFn>(IdSysGetDriverAndBranchVersion)(out var v, branch) == Ok ? $"{v / 100}.{v % 100:00}" : null;
    }

    /// <summary>
    /// VRR state of a display (GDI name like \\.\DISPLAY1), null if unknown. NV_GET_VRR_INFO_V1 bit fields:
    /// 0 bIsVRREnabled (G-SYNC turned on for the display), 1 bIsVRRPossible, 2 bIsVRRRequested,
    /// 3 bIsVRRIndicatorEnabled, 4 bIsDisplayInVRRMode (running in VRR right now).
    /// </summary>
    public static (bool Enabled, bool Possible, bool Requested, bool InVrrMode)? VrrInfo(string gdiName)
    {
        if (!Available) return null;
        try
        {
            if (Fn<DisplayIdFn>(IdDispGetDisplayIdByDisplayName)(gdiName, out var id) != Ok) return null;
            var info = new byte[VrrInfoSize];
            BitConverter.GetBytes(VrrInfoVersion).CopyTo(info, 0);
            if (Fn<VrrInfoFn>(IdDispGetVrrInfo)(id, info) != Ok) return null;
            var bits = BitConverter.ToUInt32(info, 4);
            return ((bits & 0x1) != 0, (bits & 0x2) != 0, (bits & 0x4) != 0, (bits & 0x10) != 0);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>A DRS session; settings are read from and written to one profile (global/base or a game's profile).</summary>
    public sealed class Session : IDisposable
    {
        private readonly IntPtr _session;

        public Session()
        {
            if (!Available) throw new InvalidOperationException("NVIDIA driver (NVAPI) not available");
            Check(Fn<OutHandle>(IdDrsCreateSession)(out _session), "DRS_CreateSession");
            Check(Fn<Handle1>(IdDrsLoadSettings)(_session), "DRS_LoadSettings");
        }

        /// <summary>Base profile = the global settings shown as "Global Settings" in the NVIDIA Control Panel.</summary>
        public IntPtr BaseProfile()
        {
            Check(Fn<HandleOut>(IdDrsGetBaseProfile)(_session, out var p), "DRS_GetBaseProfile");
            return p;
        }

        /// <summary>The profile a game executable belongs to; creates "PCOptimizer: &lt;exe&gt;" when the driver has none.</summary>
        public IntPtr ProfileForExe(string exePath, bool create)
        {
            var exe = Path.GetFileName(exePath).ToLowerInvariant();
            var app = new byte[ApplicationSize];
            BitConverter.GetBytes(ApplicationVersion).CopyTo(app, 0);
            var status = Fn<FindAppFn>(IdDrsFindApplicationByName)(_session, Unicode(exe), out var profile, app);
            if (status == Ok && profile != IntPtr.Zero) return profile;
            if (!create) return IntPtr.Zero;

            var name = $"PCOptimizer: {exe}";
            if (Fn<FindProfileFn>(IdDrsFindProfileByName)(_session, Unicode(name), out profile) == Ok && profile != IntPtr.Zero) return profile;
            var info = new byte[ProfileSize];
            BitConverter.GetBytes(ProfileVersion).CopyTo(info, 0);
            Unicode(name).CopyTo(info, 4);
            Check(Fn<CreateProfileFn>(IdDrsCreateProfile)(_session, info, out profile), "DRS_CreateProfile");
            var newApp = new byte[ApplicationSize];
            BitConverter.GetBytes(ApplicationVersion).CopyTo(newApp, 0);
            Unicode(exe).CopyTo(newApp, 8); // appName after version + isPredefined
            Check(Fn<CreateAppFn>(IdDrsCreateApplication)(_session, profile, newApp), "DRS_CreateApplication");
            return profile;
        }

        /// <summary>
        /// GetSetting with the layout the driver accepts. Returns the status; on success the buffer and the offset of the
        /// current value are valid. NVAPI_SETTING_NOT_FOUND (-160) means neither the profile nor its parents set it.
        /// </summary>
        private int GetRaw(IntPtr profile, uint settingId, out byte[] buffer, out int currentOffset)
        {
            var status = IncompatibleStructVersion;
            buffer = [];
            currentOffset = 0;
            for (var i = PreferredLayout; i < SettingLayouts.Length; i++)
            {
                var (size, cur) = SettingLayouts[i];
                buffer = NewSetting(size);
                currentOffset = cur;
                status = Fn<GetSettingFn>(IdDrsGetSetting)(_session, profile, settingId, buffer);
                if (status != IncompatibleStructVersion)
                {
                    PreferredLayout = i;
                    return status;
                }
            }
            return status;
        }

        /// <summary>DWORD value stored in this profile, or null when the profile does not set it (driver default applies).</summary>
        public uint? GetOwn(IntPtr profile, uint settingId)
        {
            if (GetRaw(profile, settingId, out var s, out var cur) != Ok) return null;
            // settingLocation 0 = current profile; anything else means it is inherited.
            if (BitConverter.ToInt32(s, OffSettingLocation) != 0) return null;
            return BitConverter.ToUInt32(s, cur);
        }

        /// <summary>Raw NVAPI status of a GetSetting call (diagnostics and tests).</summary>
        public int GetStatus(IntPtr profile, uint settingId, out uint value, out int location)
        {
            var status = GetRaw(profile, settingId, out var s, out var cur);
            value = s.Length > cur + 4 ? BitConverter.ToUInt32(s, cur) : 0;
            location = s.Length > OffSettingLocation + 4 ? BitConverter.ToInt32(s, OffSettingLocation) : -1;
            return status;
        }

        /// <summary>Effective DWORD value for this profile (own or inherited), or null when unavailable.</summary>
        public uint? GetEffective(IntPtr profile, uint settingId) =>
            GetRaw(profile, settingId, out var s, out var cur) == Ok ? BitConverter.ToUInt32(s, cur) : null;

        public void Set(IntPtr profile, uint settingId, uint value)
        {
            var status = IncompatibleStructVersion;
            for (var i = PreferredLayout; i < SettingLayouts.Length && status == IncompatibleStructVersion; i++)
            {
                var (size, cur) = SettingLayouts[i];
                var s = NewSetting(size);
                BitConverter.GetBytes(settingId).CopyTo(s, OffSettingId);
                BitConverter.GetBytes(0).CopyTo(s, OffSettingType); // NVDRS_DWORD_TYPE
                BitConverter.GetBytes(value).CopyTo(s, cur);
                status = Fn<SetSettingFn>(IdDrsSetSetting)(_session, profile, s);
                if (status != IncompatibleStructVersion) PreferredLayout = i;
            }
            Check(status, "DRS_SetSetting");
        }

        /// <summary>Removes the profile's own value, so the inherited/driver default applies again.</summary>
        public void Delete(IntPtr profile, uint settingId)
        {
            var status = Fn<DeleteSettingFn>(IdDrsDeleteProfileSetting)(_session, profile, settingId);
            // NVAPI_SETTING_NOT_FOUND (-160) means there was nothing to delete.
            if (status != Ok && status != -160) Check(status, "DRS_DeleteProfileSetting");
        }

        public void Save() => Check(Fn<Handle1>(IdDrsSaveSettings)(_session), "DRS_SaveSettings");

        public void Dispose() => Fn<Handle1>(IdDrsDestroySession)(_session);

        private static byte[] NewSetting(int size)
        {
            var s = new byte[size];
            BitConverter.GetBytes((uint)size | (1u << 16)).CopyTo(s, 0); // MAKE_NVAPI_VERSION(NVDRS_SETTING_V1, 1)
            return s;
        }
    }

    private static byte[] Unicode(string s)
    {
        var buffer = new byte[UnicodeStringBytes];
        var bytes = Encoding.Unicode.GetBytes(s);
        Array.Copy(bytes, buffer, Math.Min(bytes.Length, UnicodeStringBytes - 2));
        return buffer;
    }

    private static void Check(int status, string api)
    {
        if (status != Ok) throw new InvalidOperationException($"NVAPI {api} failed ({status})");
    }
}
