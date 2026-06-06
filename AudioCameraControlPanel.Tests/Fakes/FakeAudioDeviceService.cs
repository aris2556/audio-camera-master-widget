using AudioCameraControlPanel.Models;
using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.Tests.Fakes;

public sealed class FakeAudioDeviceService : IAudioDeviceService
{
    public event EventHandler<AudioDevicesChangedEventArgs>? DevicesChanged;

    public event EventHandler<AudioEndpointChangedEventArgs>? EndpointStateChanged;

    public List<AudioDeviceInfo> OutputDevices { get; } = new();

    public List<AudioDeviceInfo> InputDevices { get; } = new();

    public Dictionary<string, AudioEndpointState> EndpointStates { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, double?> PeakLevels { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> VolumeFailures { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> MuteFailures { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<SetVolumeCall> SetVolumeCalls { get; } = new();

    public List<SetMuteCall> SetMuteCalls { get; } = new();

    public string? DefaultOutputDeviceId { get; set; }

    public string? DefaultInputDeviceId { get; set; }

    public Exception? GetOutputDevicesException { get; set; }

    public Exception? GetInputDevicesException { get; set; }

    public Exception? GetEndpointStateException { get; set; }

    public Exception? GetPeakLevelException { get; set; }

    public string? SetVolumeFailureMessage { get; set; }

    public string? SetMuteFailureMessage { get; set; }

    public bool IsDisposed { get; private set; }

    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices()
    {
        if (GetOutputDevicesException is not null)
        {
            throw GetOutputDevicesException;
        }

        return OutputDevices.ToList();
    }

    public IReadOnlyList<AudioDeviceInfo> GetInputDevices()
    {
        if (GetInputDevicesException is not null)
        {
            throw GetInputDevicesException;
        }

        return InputDevices.ToList();
    }

    public string? GetDefaultOutputDeviceId()
    {
        return DefaultOutputDeviceId ?? OutputDevices.FirstOrDefault(device => device.IsDefault)?.Id;
    }

    public string? GetDefaultInputDeviceId()
    {
        return DefaultInputDeviceId ?? InputDevices.FirstOrDefault(device => device.IsDefault)?.Id;
    }

    public AudioEndpointState GetEndpointState(string deviceId)
    {
        if (GetEndpointStateException is not null)
        {
            throw GetEndpointStateException;
        }

        return EndpointStates.TryGetValue(deviceId, out var state)
            ? state
            : new AudioEndpointState(
                null,
                null,
                false,
                false,
                $"No endpoint state was configured for '{deviceId}'.");
    }

    public bool TrySetVolume(string deviceId, double volumePercent, out string? errorMessage)
    {
        SetVolumeCalls.Add(new SetVolumeCall(deviceId, volumePercent));

        if (SetVolumeFailureMessage is not null)
        {
            errorMessage = SetVolumeFailureMessage;
            return false;
        }

        if (VolumeFailures.TryGetValue(deviceId, out var failureMessage))
        {
            errorMessage = failureMessage;
            return false;
        }

        if (!EndpointStates.TryGetValue(deviceId, out var state) || !state.CanControlVolume)
        {
            errorMessage = "The selected endpoint does not support volume control.";
            return false;
        }

        errorMessage = null;
        EndpointStates[deviceId] = new AudioEndpointState(
            Math.Clamp(Math.Round(volumePercent, 0), 0, 100),
            state.IsMuted,
            state.CanControlVolume,
            state.CanControlMute,
            state.ErrorMessage);
        return true;
    }

    public bool TrySetMute(string deviceId, bool isMuted, out string? errorMessage)
    {
        SetMuteCalls.Add(new SetMuteCall(deviceId, isMuted));

        if (SetMuteFailureMessage is not null)
        {
            errorMessage = SetMuteFailureMessage;
            return false;
        }

        if (MuteFailures.TryGetValue(deviceId, out var failureMessage))
        {
            errorMessage = failureMessage;
            return false;
        }

        if (!EndpointStates.TryGetValue(deviceId, out var state) || !state.CanControlMute)
        {
            errorMessage = "The selected endpoint does not support mute control.";
            return false;
        }

        errorMessage = null;
        EndpointStates[deviceId] = new AudioEndpointState(
            state.VolumePercent,
            isMuted,
            state.CanControlVolume,
            state.CanControlMute,
            state.ErrorMessage);
        return true;
    }

    public double? GetPeakLevelPercent(string deviceId)
    {
        if (GetPeakLevelException is not null)
        {
            throw GetPeakLevelException;
        }

        return PeakLevels.TryGetValue(deviceId, out var peakLevel)
            ? peakLevel
            : null;
    }

    public AudioDeviceInfo AddOutputDevice(string id, string name, bool isDefault = false)
    {
        var device = new AudioDeviceInfo(id, name, AudioDirection.Output, isDefault);
        OutputDevices.Add(device);
        return device;
    }

    public AudioDeviceInfo AddInputDevice(string id, string name, bool isDefault = false)
    {
        var device = new AudioDeviceInfo(id, name, AudioDirection.Input, isDefault);
        InputDevices.Add(device);
        return device;
    }

    public void SetEndpointState(
        string deviceId,
        double? volumePercent,
        bool? isMuted,
        bool canControlVolume = true,
        bool canControlMute = true,
        string? errorMessage = null)
    {
        EndpointStates[deviceId] = new AudioEndpointState(
            volumePercent,
            isMuted,
            canControlVolume,
            canControlMute,
            errorMessage);
    }

    public void RaiseDevicesChanged(AudioDirection? direction, string? deviceId = null, bool isDefaultChange = false)
    {
        DevicesChanged?.Invoke(this, new AudioDevicesChangedEventArgs(direction, deviceId, isDefaultChange));
    }

    public void RaiseEndpointStateChanged(string deviceId)
    {
        EndpointStateChanged?.Invoke(this, new AudioEndpointChangedEventArgs(deviceId));
    }

    public void Dispose()
    {
        IsDisposed = true;
    }

    public readonly record struct SetVolumeCall(string DeviceId, double VolumePercent);

    public readonly record struct SetMuteCall(string DeviceId, bool IsMuted);
}
