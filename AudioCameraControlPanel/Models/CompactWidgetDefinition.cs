namespace AudioCameraControlPanel.Models;

public sealed class CompactWidgetDefinition
{
    public CompactWidgetDefinition(
        CompactWidgetKind kind,
        string title,
        string description)
    {
        Kind = kind;
        Title = title;
        Description = description;
    }

    public CompactWidgetKind Kind { get; }

    public string Title { get; }

    public string Description { get; }
}
