using System.Diagnostics;

namespace AudioCameraControlPanel.Services;

public sealed class SettingsLauncherService : ISettingsLauncherService
{
    public static string GetSettingsUri(WindowsSettingsPage page)
    {
        return page switch
        {
            WindowsSettingsPage.Sound => "ms-settings:sound",
            WindowsSettingsPage.AppVolume => "ms-settings:apps-volume",
            WindowsSettingsPage.Camera => "ms-settings:camera",
            WindowsSettingsPage.CameraPrivacy => "ms-settings:privacy-webcam",
            WindowsSettingsPage.MicrophonePrivacy => "ms-settings:privacy-microphone",
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };
    }

    public void Launch(WindowsSettingsPage page)
    {
        var uri = GetSettingsUri(page);
        Process.Start(new ProcessStartInfo
        {
            FileName = uri,
            UseShellExecute = true
        });
    }
}
