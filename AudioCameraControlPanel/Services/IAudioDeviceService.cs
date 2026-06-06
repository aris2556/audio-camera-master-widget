using AudioCameraControlPanel.Models;

namespace AudioCameraControlPanel.Services;

public interface IAudioDeviceService : IDisposable
{
    event EventHandler<AudioDevicesChangedEventArgs>? DevicesChanged;

    event EventHandler<AudioEndpointChangedEventArgs>? EndpointStateChanged;

    IReadOnlyList<AudioDeviceInfo> GetOutputDevices();

    IReadOnlyList<AudioDeviceInfo> GetInputDevices();

    string? GetDefaultOutputDeviceId();

    string? GetDefaultInputDeviceId();

    AudioEndpointState GetEndpointState(string deviceId);

    bool TrySetVolume(string deviceId, double volumePercent, out string? errorMessage);

    bool TrySetMute(string deviceId, bool isMuted, out string? errorMessage);

    double? GetPeakLevelPercent(string deviceId);
}
