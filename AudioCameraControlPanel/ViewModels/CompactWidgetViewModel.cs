using AudioCameraControlPanel.Models;
using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.ViewModels;

public sealed class CompactWidgetViewModel
{
    public CompactWidgetViewModel(MainViewModel main, CompactWidgetKind kind)
    {
        Main = main;
        Kind = kind;
        Definition = CompactWidgetCatalog.Get(kind);
    }

    public MainViewModel Main { get; }

    public CompactWidgetKind Kind { get; }

    public CompactWidgetDefinition Definition { get; }

    public bool IsMaster => Kind == CompactWidgetKind.Master;

    public bool IsOutput => Kind == CompactWidgetKind.Output;

    public bool IsInput => Kind == CompactWidgetKind.Input;

    public bool IsCamera => Kind == CompactWidgetKind.Camera;

    public bool IsSettings => Kind == CompactWidgetKind.Settings;
}
