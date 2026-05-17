using System;
using System.Runtime.InteropServices;

namespace AudioSwitcher;

internal enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
internal enum ERole   { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

// MMDeviceEnumerator CLSID
[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class MMDeviceEnumeratorComObject { }

[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask,
                           out IMMDeviceCollection ppDevices);
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role,
                                out IMMDevice ppEndpoint);
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId,
                  out IMMDevice ppDevice);
    int RegisterEndpointNotificationCallback(IntPtr pClient);
    int UnregisterEndpointNotificationCallback(IntPtr pClient);
}

[ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    int GetCount(out uint pcDevices);
    int Item(uint nDevice, out IMMDevice ppDevice);
}

[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams,
                 [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
    int GetState(out int pdwState);
}

[ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    int GetCount(out uint cProps);
    int GetAt(uint iProp, out PropertyKey pkey);
    int GetValue(ref PropertyKey key, out PropVariant pv);
    int SetValue(ref PropertyKey key, ref PropVariant pv);
    int Commit();
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey
{
    public Guid fmtid;
    public int  pid;

    // PKEY_Device_FriendlyName
    public static readonly PropertyKey FriendlyName = new()
    {
        fmtid = new Guid("{a45c254e-df1c-4efd-8020-67d146a850e0}"),
        pid   = 14,
    };
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal struct PropVariant
{
    [FieldOffset(0)] public short   vt;
    [FieldOffset(8)] public IntPtr  pwszVal;

    public string? GetString() =>
        vt == 31 /*VT_LPWSTR*/ ? Marshal.PtrToStringUni(pwszVal) : null;

    public void Clear() => PropVariantClear(ref this);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);
}

// IPolicyConfig — 비공개 COM 인터페이스, vtable 순서가 중요합니다
[ComImport, Guid("f8679f50-850a-41cf-9c72-430f290290c8"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    [PreserveSig] int GetMixFormat(
        [MarshalAs(UnmanagedType.LPWStr)] string dev, IntPtr ppFormat);
    [PreserveSig] int GetDeviceFormat(
        [MarshalAs(UnmanagedType.LPWStr)] string dev,
        [MarshalAs(UnmanagedType.Bool)] bool bDefault, IntPtr ppFormat);
    [PreserveSig] int ResetDeviceFormat(
        [MarshalAs(UnmanagedType.LPWStr)] string dev);
    [PreserveSig] int SetDeviceFormat(
        [MarshalAs(UnmanagedType.LPWStr)] string dev,
        IntPtr pEndpointFormat, IntPtr MixFormat);
    [PreserveSig] int GetProcessingPeriod(
        [MarshalAs(UnmanagedType.LPWStr)] string dev,
        [MarshalAs(UnmanagedType.Bool)] bool bDefault,
        IntPtr pmftDefault, IntPtr pmftMinimum);
    [PreserveSig] int SetProcessingPeriod(
        [MarshalAs(UnmanagedType.LPWStr)] string dev, IntPtr pmftPeriod);
    [PreserveSig] int GetShareMode(
        [MarshalAs(UnmanagedType.LPWStr)] string dev, IntPtr pMode);
    [PreserveSig] int SetShareMode(
        [MarshalAs(UnmanagedType.LPWStr)] string dev, IntPtr mode);
    [PreserveSig] int GetPropertyValue(
        [MarshalAs(UnmanagedType.LPWStr)] string dev,
        [MarshalAs(UnmanagedType.Bool)] bool bFxStore,
        IntPtr key, IntPtr pv);
    [PreserveSig] int SetPropertyValue(
        [MarshalAs(UnmanagedType.LPWStr)] string dev,
        [MarshalAs(UnmanagedType.Bool)] bool bFxStore,
        IntPtr key, IntPtr pv);
    [PreserveSig] int SetDefaultEndpoint(   // <-- 핵심 메서드
        [MarshalAs(UnmanagedType.LPWStr)] string dev, ERole role);
    [PreserveSig] int SetEndpointVisibility(
        [MarshalAs(UnmanagedType.LPWStr)] string dev,
        [MarshalAs(UnmanagedType.Bool)] bool bVisible);
}

// PolicyConfigClient CLSID
[ComImport, Guid("294935CE-F637-4E7C-A41B-AB255460B862")]
internal class PolicyConfigComObject { }
