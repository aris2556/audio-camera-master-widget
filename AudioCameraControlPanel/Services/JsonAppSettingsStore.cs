using System.IO;
using System.Text.Json;

namespace AudioCameraControlPanel.Services;

public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private const string FileName = "settings.json";
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _settingsDirectory;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public JsonAppSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AudioCameraControlPanel"))
    {
    }

    public JsonAppSettingsStore(string settingsDirectory)
    {
        _settingsDirectory = settingsDirectory;
    }

    private string SettingsPath => Path.Combine(_settingsDirectory, FileName);

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            await using var stream = File.OpenRead(SettingsPath);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await _saveLock.WaitAsync();
        string? tempPath = null;
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            tempPath = Path.Combine(_settingsDirectory, $"{FileName}.{Guid.NewGuid():N}.tmp");
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions);
            }

            if (File.Exists(SettingsPath))
            {
                File.Replace(tempPath, SettingsPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, SettingsPath);
            }

            tempPath = null;
        }
        finally
        {
            if (tempPath is not null && File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            _saveLock.Release();
        }
    }
}
