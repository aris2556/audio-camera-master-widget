namespace AudioCameraControlPanel.Services;

public sealed class AudioEndpointChangedEventArgs : EventArgs
{
    public AudioEndpointChangedEventArgs(string deviceId)
    {
        DeviceId = deviceId;
    }

    public string DeviceId { get; }
}
