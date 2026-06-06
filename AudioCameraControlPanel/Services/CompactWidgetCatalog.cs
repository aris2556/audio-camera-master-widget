using AudioCameraControlPanel.Models;

namespace AudioCameraControlPanel.Services;

public static class CompactWidgetCatalog
{
    public static IReadOnlyList<CompactWidgetDefinition> All { get; } =
    [
        new(CompactWidgetKind.Master, "Master widget", "Output, microphone, and settings. No camera preview."),
        new(CompactWidgetKind.Output, "Output widget", "Output device, volume, and mute."),
        new(CompactWidgetKind.Input, "Input widget", "Microphone level, meter, and mute."),
        new(CompactWidgetKind.Camera, "Camera widget", "Camera picker and compact preview."),
        new(CompactWidgetKind.Settings, "Settings widget", "Native Windows settings shortcuts.")
    ];

    public static CompactWidgetDefinition Get(CompactWidgetKind kind)
    {
        return All.First(widget => widget.Kind == kind);
    }
}
