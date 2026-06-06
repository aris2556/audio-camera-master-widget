using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.Tests.Fakes;

public sealed class FakeSettingsLauncherService : ISettingsLauncherService
{
    public List<WindowsSettingsPage> LaunchedPages { get; } = new();

    public Exception? LaunchException { get; set; }

    public void Launch(WindowsSettingsPage page)
    {
        LaunchedPages.Add(page);

        if (LaunchException is not null)
        {
            throw LaunchException;
        }
    }
}
