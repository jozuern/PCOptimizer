using System.Runtime.InteropServices;

namespace Optimizer.Core.Interop;

/// <summary>P/Invoke declarations. Everything here only reads system state; changes go through NativeWrite.</summary>
internal static partial class Native
{
    // ---------- kernel32 ----------
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetFirmwareType(out int firmwareType);

    internal const int FirmwareTypeBios = 1, FirmwareTypeUefi = 2;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

    internal const ushort ImageFileMachineAmd64 = 0x8664, ImageFileMachineArm64 = 0xAA64;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetLogicalProcessorInformationEx(int relationshipType, IntPtr buffer, ref uint returnedLength);

    internal const int RelationProcessorCore = 0, RelationCache = 2, RelationAll = 0xFFFF;

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

    // ---------- powrprof ----------
    [LibraryImport("powrprof.dll")]
    internal static partial uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [LibraryImport("powrprof.dll")]
    internal static partial uint PowerReadACValueIndex(IntPtr rootPowerKey, in Guid schemeGuid, in Guid subGroupOfPowerSettingsGuid, in Guid powerSettingGuid, out uint acValueIndex);

    [LibraryImport("powrprof.dll")]
    internal static partial uint PowerReadDCValueIndex(IntPtr rootPowerKey, in Guid schemeGuid, in Guid subGroupOfPowerSettingsGuid, in Guid powerSettingGuid, out uint dcValueIndex);

    [LibraryImport("powrprof.dll")]
    internal static partial uint PowerReadFriendlyName(IntPtr rootPowerKey, in Guid schemeGuid, IntPtr subGroupOfPowerSettingsGuid, IntPtr powerSettingGuid, [Out] byte[]? buffer, ref uint bufferSize);

    [LibraryImport("powrprof.dll")]
    internal static partial uint PowerReadACValue(IntPtr rootPowerKey, in Guid schemeGuid, in Guid subGroupOfPowerSettingsGuid, in Guid powerSettingGuid, out uint type, [Out] byte[]? buffer, ref uint bufferSize);

    [LibraryImport("powrprof.dll")]
    internal static partial uint CallNtPowerInformation(int informationLevel, IntPtr inputBuffer, uint inputBufferLength, [Out] byte[] outputBuffer, uint outputBufferLength);

    internal const int SystemPowerCapabilities = 4;

    [LibraryImport("kernel32.dll")]
    internal static partial IntPtr LocalFree(IntPtr mem);

    // ---------- wtsapi32 / netapi32 ----------
    [LibraryImport("wtsapi32.dll", EntryPoint = "WTSQuerySessionInformationW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool WTSQuerySessionInformation(IntPtr server, int sessionId, int infoClass, out IntPtr buffer, out uint bytesReturned);

    [LibraryImport("wtsapi32.dll")]
    internal static partial void WTSFreeMemory(IntPtr memory);

    internal const int WtsCurrentSession = -1, WtsUserName = 5, WtsDomainName = 7;

    [LibraryImport("netapi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int NetGetJoinInformation(string? server, out IntPtr nameBuffer, out int bufferType);

    [LibraryImport("netapi32.dll")]
    internal static partial int NetApiBufferFree(IntPtr buffer);

    internal const int NetSetupDomainName = 3;

    // ---------- user32: display configuration (CCD API) ----------
    internal const uint QdcOnlyActivePaths = 0x2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID { public uint LowPart; public int HighPart; public long Value => ((long)HighPart << 32) | LowPart; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISPLAYCONFIG_RATIONAL { public uint Numerator; public uint Denominator; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint scanLineOrdering;
        public int targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    /// <summary>DISPLAYCONFIG_MODE_INFO (64 bytes). Union members read through explicit offsets.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    internal struct DISPLAYCONFIG_MODE_INFO
    {
        [FieldOffset(0)] public uint infoType;          // 1 = source, 2 = target, 3 = desktop image
        [FieldOffset(4)] public uint id;
        [FieldOffset(8)] public LUID adapterId;
        // source mode
        [FieldOffset(16)] public uint sourceWidth;
        [FieldOffset(20)] public uint sourceHeight;
        [FieldOffset(28)] public int sourcePositionX;  // DISPLAYCONFIG_SOURCE_MODE.position (POINTL), desktop coordinates
        [FieldOffset(32)] public int sourcePositionY;
        // target mode (DISPLAYCONFIG_VIDEO_SIGNAL_INFO)
        [FieldOffset(16)] public ulong pixelRate;
        [FieldOffset(32)] public DISPLAYCONFIG_RATIONAL vSyncFreq;
        [FieldOffset(40)] public uint activeWidth;
        [FieldOffset(44)] public uint activeHeight;
    }

    internal const uint ModeInfoTypeSource = 1, ModeInfoTypeTarget = 2;

    [LibraryImport("user32.dll")]
    internal static partial int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [LibraryImport("user32.dll")]
    internal static partial int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements, [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, IntPtr currentTopologyId);

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    internal const uint DeviceInfoGetSourceName = 1, DeviceInfoGetTargetName = 2, DeviceInfoGetAdapterName = 4, DeviceInfoGetAdvancedColorInfo = 9;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint flags;
        public uint outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string monitorFriendlyDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string monitorDevicePath;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DISPLAYCONFIG_ADAPTER_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string adapterDevicePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint value;
        public uint colorEncoding;
        public uint bitsPerColorChannel;
    }

    [DllImport("user32.dll")] internal static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);
    [DllImport("user32.dll")] internal static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);
    [DllImport("user32.dll")] internal static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_ADAPTER_NAME requestPacket);
    [DllImport("user32.dll")] internal static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO requestPacket);

    internal const uint OutputTechnologyInternal = 0x80000000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }

    internal const uint DmInterlaced = 0x2;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool EnumDisplaySettingsEx(string deviceName, int modeNum, ref DEVMODE devMode, uint flags);

    // ---------- cfgmgr32 ----------
    internal const uint CrSuccess = 0;

    [StructLayout(LayoutKind.Sequential)]
    internal struct DEVPROPKEY { public Guid fmtid; public uint pid; }

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint CM_Locate_DevNodeW(out uint devInst, string deviceId, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_Parent(out uint parentDevInst, uint devInst, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint CM_Get_Device_IDW(uint devInst, char[] buffer, int bufferLen, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_DevNode_PropertyW(uint devInst, ref DEVPROPKEY propertyKey, out uint propertyType, byte[]? propertyBuffer, ref uint propertyBufferSize, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_First_Log_Conf(out nuint logConf, uint devInst, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_Next_Res_Des(out nuint resDes, nuint current, uint forResource, out uint resourceId, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_Res_Des_Data_Size(out uint size, nuint resDes, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_Res_Des_Data(nuint resDes, byte[] buffer, uint bufferLen, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Free_Res_Des_Handle(nuint resDes);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Free_Log_Conf_Handle(nuint logConf);

    internal const uint AllocLogConf = 2, ResTypeAll = 0, ResTypeMem = 1, ResTypeMemLarge = 7;
}
