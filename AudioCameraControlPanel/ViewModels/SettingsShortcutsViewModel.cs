using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.ViewModels;

public sealed class SettingsShortcutsViewModel : ObservableObject
{
    private readonly ISettingsLauncherService _settingsLauncherService;
    private string _statusText = string.Empty;

    public SettingsShortcutsViewModel(ISettingsLauncherService settingsLauncherService)
    {
        _settingsLauncherService = settingsLauncherService;

        OpenSoundSettingsCommand = new RelayCommand(() => LaunchSettings(WindowsSettingsPage.Sound));
        OpenAppVolumeSettingsCommand = new RelayCommand(() => LaunchSettings(WindowsSettingsPage.AppVolume));
        OpenCameraSettingsCommand = new RelayCommand(() => LaunchSettings(WindowsSettingsPage.Camera));
        OpenCameraPrivacyCommand = new RelayCommand(() => LaunchSettings(WindowsSettingsPage.CameraPrivacy));
        OpenMicrophonePrivacyCommand = new RelayCommand(() => LaunchSettings(WindowsSettingsPage.MicrophonePrivacy));
    }

    public event EventHandler<string>? LaunchFailed;

    public RelayCommand OpenSoundSettingsCommand { get; }

    public RelayCommand OpenAppVolumeSettingsCommand { get; }

    public RelayCommand OpenCameraSettingsCommand { get; }

    public RelayCommand OpenCameraPrivacyCommand { get; }

    public RelayCommand OpenMicrophonePrivacyCommand { get; }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private void LaunchSettings(WindowsSettingsPage page)
    {
        try
        {
            _settingsLauncherService.Launch(page);
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to open Windows Settings. {ex.Message}";
            LaunchFailed?.Invoke(this, StatusText);
        }
    }
}
