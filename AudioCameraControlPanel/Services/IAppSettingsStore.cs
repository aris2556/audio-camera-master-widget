namespace AudioCameraControlPanel.Services;

public interface IAppSettingsStore
{
    Task<AppSettings> LoadAsync();

    Task SaveAsync(AppSettings settings);
}
