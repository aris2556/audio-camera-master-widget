using AudioCameraControlPanel.Services;
using System.IO;

namespace AudioCameraControlPanel.Tests;

[TestClass]
public sealed class JsonAppSettingsStoreTests
{
    [TestMethod]
    public async Task SaveAsyncPersistsLastSelectedDeviceIds()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AudioCameraControlPanelTests", Guid.NewGuid().ToString("N"));
        var store = new JsonAppSettingsStore(directory);
        var settings = new AppSettings
        {
            LastOutputDeviceId = "output-id",
            LastInputDeviceId = "input-id",
            LastCameraDeviceId = "camera-id"
        };

        await store.SaveAsync(settings);

        var loaded = await new JsonAppSettingsStore(directory).LoadAsync();

        Assert.AreEqual("output-id", loaded.LastOutputDeviceId);
        Assert.AreEqual("input-id", loaded.LastInputDeviceId);
        Assert.AreEqual("camera-id", loaded.LastCameraDeviceId);
    }

    [TestMethod]
    public async Task SaveAsyncOverwritesExistingSettingsWithValidJson()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AudioCameraControlPanelTests", Guid.NewGuid().ToString("N"));
        var store = new JsonAppSettingsStore(directory);

        await store.SaveAsync(new AppSettings { LastOutputDeviceId = "old-output" });
        await store.SaveAsync(new AppSettings
        {
            LastOutputDeviceId = "new-output",
            LastInputDeviceId = "new-input",
            LastCameraDeviceId = "new-camera"
        });

        var loaded = await store.LoadAsync();

        Assert.AreEqual("new-output", loaded.LastOutputDeviceId);
        Assert.AreEqual("new-input", loaded.LastInputDeviceId);
        Assert.AreEqual("new-camera", loaded.LastCameraDeviceId);
    }
}
