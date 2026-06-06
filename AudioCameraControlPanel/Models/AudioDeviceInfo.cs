namespace AudioCameraControlPanel.Models;

public sealed class AudioDeviceInfo
{
    public AudioDeviceInfo(string id, string name, AudioDirection direction, bool isDefault)
    {
        Id = id;
        Name = name;
        Direction = direction;
        IsDefault = isDefault;
    }

    public string Id { get; }

    public string Name { get; }

    public AudioDirection Direction { get; }

    public bool IsDefault { get; }

    public string DisplayName => IsDefault ? $"{Name} (default)" : Name;
}
