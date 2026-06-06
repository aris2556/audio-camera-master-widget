namespace AudioCameraControlPanel.Models;

public sealed class AudioEndpointState
{
    public AudioEndpointState(
        double? volumePercent,
        bool? isMuted,
        bool canControlVolume,
        bool canControlMute,
        string? errorMessage = null)
    {
        VolumePercent = volumePercent;
        IsMuted = isMuted;
        CanControlVolume = canControlVolume;
        CanControlMute = canControlMute;
        ErrorMessage = errorMessage;
    }

    public double? VolumePercent { get; }

    public bool? IsMuted { get; }

    public bool CanControlVolume { get; }

    public bool CanControlMute { get; }

    public string? ErrorMessage { get; }
}
