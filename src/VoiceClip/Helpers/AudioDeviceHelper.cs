using System.Runtime.InteropServices;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

namespace VoiceClip.Helpers;

public record AudioDevice(string Id, string Name);

public static class AudioDeviceHelper
{
    public static async Task<IReadOnlyList<AudioDevice>> GetInputDevicesAsync()
    {
        try
        {
            var selector = MediaDevice.GetAudioCaptureSelector();
            var devices = await DeviceInformation.FindAllAsync(selector);
            return devices
                .Where(d => d.IsEnabled)
                .Select(d => new AudioDevice(d.Id, d.Name))
                .ToList()
                .AsReadOnly();
        }
        catch
        {
            return Array.Empty<AudioDevice>();
        }
    }

    public static string? GetDefaultCommunicationDeviceId()
    {
        try
        {
            return MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Communications);
        }
        catch
        {
            return null;
        }
    }

    public static bool SetDefaultCommunicationDevice(string deviceId)
    {
        try
        {
            var policyConfig = (IPolicyConfig)new PolicyConfigClient();
            return policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications) == 0;
        }
        catch
        {
            return false;
        }
    }

    [ComImport]
    [Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat(string pszDeviceName, IntPtr ppFormat);
        [PreserveSig] int GetDeviceFormat(string pszDeviceName, int bDefault, IntPtr ppFormat);
        [PreserveSig] int ResetDeviceFormat(string pszDeviceName);
        [PreserveSig] int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr mixFormat);
        [PreserveSig] int GetProcessingPeriod(string pszDeviceName, int bDefault, IntPtr pmftDefaultPeriod, IntPtr pmftMinimumPeriod);
        [PreserveSig] int SetProcessingPeriod(string pszDeviceName, IntPtr pmftPeriod);
        [PreserveSig] int GetShareMode(string pszDeviceName, IntPtr pMode);
        [PreserveSig] int SetShareMode(string pszDeviceName, IntPtr mode);
        [PreserveSig] int GetPropertyValue(string pszDeviceName, int bFxStore, IntPtr key, IntPtr pv);
        [PreserveSig] int SetPropertyValue(string pszDeviceName, int bFxStore, IntPtr key, IntPtr pv);
        [PreserveSig] int SetDefaultEndpoint(string pszDeviceName, ERole role);
        [PreserveSig] int SetEndpointVisibility(string pszDeviceName, int bVisible);
    }

    [ComImport]
    [Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    private class PolicyConfigClient { }

    private enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }
}
