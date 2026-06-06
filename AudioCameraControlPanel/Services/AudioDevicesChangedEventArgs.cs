using AudioCameraControlPanel.Models;

namespace AudioCameraControlPanel.Services;

public sealed class AudioDevicesChangedEventArgs : EventArgs
{
    public AudioDevicesChangedEventArgs(AudioDirection? direction, string? deviceId = null, bool isDefaultChange = false)
    {
        Direction = direction;
        DeviceId = deviceId;
        IsDefaultChange = isDefaultChange;
    }

    public AudioDirection? Direction { get; }

    public string? DeviceId { get; }

    public bool IsDefaultChange { get; }
}
