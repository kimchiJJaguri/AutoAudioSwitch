using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AutoAudioSwitch;

internal sealed record AudioDevice(string Id, string Name);

internal sealed class AudioDeviceManager : IDisposable
{
    private const int DEVICE_STATE_ACTIVE = 0x1;
    private const int STGM_READ          = 0x0;

    // PolicyConfigElevatedClient CLSID — 일반 Client가 실패할 때 대안
    private static readonly Guid ClsidPolicyConfigClient =
        new("294935CE-F637-4E7C-A41B-AB255460B862");
    private static readonly Guid ClsidPolicyConfigElevated =
        new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");

    private readonly IMMDeviceEnumerator _enumerator =
        (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();

    public List<AudioDevice> GetPlaybackDevices()
    {
        var result = new List<AudioDevice>();
        _enumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE_ACTIVE,
                                       out var collection);
        collection.GetCount(out uint count);
        for (uint i = 0; i < count; i++)
        {
            collection.Item(i, out var device);
            if (TryGetDeviceInfo(device, out var info))
                result.Add(info!);
        }
        return result;
    }

    public string? GetDefaultDeviceId()
    {
        try
        {
            _enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole,
                                                out var device);
            device.GetId(out string id);
            return id;
        }
        catch { return null; }
    }

    // 실패 시 COMException을 호출자에게 그대로 전달합니다
    public void SetDefaultDevice(string deviceId)
    {
        var policy = CreatePolicyConfig();
        try
        {
            int hr;
            hr = policy.SetDefaultEndpoint(deviceId, ERole.eConsole);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            hr = policy.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            hr = policy.SetDefaultEndpoint(deviceId, ERole.eCommunications);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        }
        finally
        {
            Marshal.ReleaseComObject(policy);
        }
    }

    public AudioDevice? CycleToNextDevice()
    {
        var devices   = GetPlaybackDevices();
        if (devices.Count == 0) return null;

        var defaultId = GetDefaultDeviceId();
        int current   = devices.FindIndex(d => d.Id == defaultId);
        int next      = (current + 1) % devices.Count;

        var target = devices[next];
        SetDefaultDevice(target.Id); // 예외는 호출자가 처리
        return target;
    }

    // 일반 PolicyConfigClient → 실패 시 Elevated로 재시도
    private static IPolicyConfig CreatePolicyConfig()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(ClsidPolicyConfigClient, throwOnError: true)!;
            return (IPolicyConfig)Activator.CreateInstance(type)!;
        }
        catch
        {
            var type = Type.GetTypeFromCLSID(ClsidPolicyConfigElevated, throwOnError: true)!;
            return (IPolicyConfig)Activator.CreateInstance(type)!;
        }
    }

    private static bool TryGetDeviceInfo(IMMDevice device, out AudioDevice? info)
    {
        info = null;
        try
        {
            device.GetId(out string id);
            device.OpenPropertyStore(STGM_READ, out var store);
            var key = PropertyKey.FriendlyName;
            store.GetValue(ref key, out var pv);
            var name = pv.GetString() ?? id;
            pv.Clear();
            Marshal.ReleaseComObject(store);
            info = new AudioDevice(id, name);
            return true;
        }
        catch { return false; }
    }

    public void Dispose() => Marshal.ReleaseComObject(_enumerator);
}
