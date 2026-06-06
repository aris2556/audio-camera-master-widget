namespace AudioCameraControlPanel.Models;

public sealed class CameraDeviceInfo
{
    public CameraDeviceInfo(string id, string name)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? "Unnamed camera" : name;
    }

    public string Id { get; }

    public string Name { get; }

    public string DisplayName => Name;
}
