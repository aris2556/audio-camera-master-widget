using System.Runtime.InteropServices;

namespace AudioCameraControlPanel.Services;

// Minimal Core Audio COM definitions used by AudioDeviceService.
// These are the public MMDevice and endpoint-volume interfaces documented by Microsoft.
internal static class CoreAudioInterop
{
    internal static readonly PropertyKey DeviceFriendlyNameKey = new(
        new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
        14);

    internal static void ThrowIfFailed(int hresult)
    {
        if (hresult < 0)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }
    }

    [DllImport("ole32.dll")]
    internal static extern int PropVariantClear(ref PropVariant propVariant);
}

internal enum EDataFlow
{
    Render,
    Capture,
    All
}

internal enum ERole
{
    Console,
    Multimedia,
    Communications
}

[Flags]
internal enum DeviceState : uint
{
    Active = 0x00000001,
    Disabled = 0x00000002,
    NotPresent = 0x00000004,
    Unplugged = 0x00000008,
    All = 0x0000000F
}

[Flags]
internal enum ClsCtx : uint
{
    InprocServer = 0x1
}

internal enum StorageAccessMode
{
    Read = 0
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey
{
    public PropertyKey(Guid formatId, int propertyId)
    {
        FormatId = formatId;
        PropertyId = propertyId;
    }

    public Guid FormatId;
    public int PropertyId;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropVariant
{
    private readonly ushort _variantType;
    private readonly ushort _reserved1;
    private readonly ushort _reserved2;
    private readonly ushort _reserved3;
    private readonly IntPtr _value;
    private readonly int _value2;

    public string? GetString()
    {
        const ushort vtLpwstr = 31;
        return _variantType == vtLpwstr ? Marshal.PtrToStringUni(_value) : null;
    }
}

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal sealed class MMDeviceEnumeratorComObject
{
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string deviceId, out IMMDevice device);

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IMMNotificationClient client);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMNotificationClient
{
    [PreserveSig]
    int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, DeviceState newState);

    [PreserveSig]
    int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [PreserveSig]
    int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [PreserveSig]
    int OnDefaultDeviceChanged(EDataFlow flow, ERole role, [MarshalAs(UnmanagedType.LPWStr)] string? defaultDeviceId);

    [PreserveSig]
    int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, PropertyKey key);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int Item(uint index, out IMMDevice device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    [PreserveSig]
    int Activate(ref Guid interfaceId, ClsCtx classContext, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);

    [PreserveSig]
    int OpenPropertyStore(StorageAccessMode accessMode, out IPropertyStore properties);

    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string deviceId);

    [PreserveSig]
    int GetState(out DeviceState state);
}

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    [PreserveSig]
    int GetCount(out uint propertyCount);

    [PreserveSig]
    int GetAt(uint propertyIndex, out PropertyKey key);

    [PreserveSig]
    int GetValue(ref PropertyKey key, out PropVariant value);

    [PreserveSig]
    int SetValue(ref PropertyKey key, ref PropVariant value);

    [PreserveSig]
    int Commit();
}

[ComImport]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioEndpointVolume
{
    // Keep this declaration order aligned with endpointvolume.h; COM dispatch uses this vtable order.
    [PreserveSig]
    int RegisterControlChangeNotify(IAudioEndpointVolumeCallback notify);

    [PreserveSig]
    int UnregisterControlChangeNotify(IAudioEndpointVolumeCallback notify);

    [PreserveSig]
    int GetChannelCount(out uint channelCount);

    [PreserveSig]
    int SetMasterVolumeLevel(float levelDb, IntPtr eventContext);

    [PreserveSig]
    int SetMasterVolumeLevelScalar(float level, IntPtr eventContext);

    [PreserveSig]
    int GetMasterVolumeLevel(out float levelDb);

    [PreserveSig]
    int GetMasterVolumeLevelScalar(out float level);

    [PreserveSig]
    int SetChannelVolumeLevel(uint channelNumber, float levelDb, IntPtr eventContext);

    [PreserveSig]
    int SetChannelVolumeLevelScalar(uint channelNumber, float level, IntPtr eventContext);

    [PreserveSig]
    int GetChannelVolumeLevel(uint channelNumber, out float levelDb);

    [PreserveSig]
    int GetChannelVolumeLevelScalar(uint channelNumber, out float level);

    [PreserveSig]
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool isMuted, IntPtr eventContext);

    [PreserveSig]
    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool isMuted);

    [PreserveSig]
    int GetVolumeStepInfo(out uint step, out uint stepCount);

    [PreserveSig]
    int VolumeStepUp(IntPtr eventContext);

    [PreserveSig]
    int VolumeStepDown(IntPtr eventContext);

    [PreserveSig]
    int QueryHardwareSupport(out uint hardwareSupportMask);

    [PreserveSig]
    int GetVolumeRange(out float minDb, out float maxDb, out float incrementDb);
}

[Guid("657804FA-D6AD-4496-8A60-352752AF4F89")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioEndpointVolumeCallback
{
    [PreserveSig]
    int OnNotify(IntPtr notifyData);
}

[ComImport]
[Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioMeterInformation
{
    [PreserveSig]
    int GetPeakValue(out float peak);

    [PreserveSig]
    int GetMeteringChannelCount(out uint channelCount);

    [PreserveSig]
    int GetChannelsPeakValues(uint channelCount, [Out] float[] peakValues);

    [PreserveSig]
    int QueryHardwareSupport(out uint hardwareSupportMask);
}
