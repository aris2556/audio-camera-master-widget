using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.Tests;

[TestClass]
public sealed class SettingsLauncherServiceTests
{
    [TestMethod]
    [DataRow(WindowsSettingsPage.Sound, "ms-settings:sound")]
    [DataRow(WindowsSettingsPage.AppVolume, "ms-settings:apps-volume")]
    [DataRow(WindowsSettingsPage.Camera, "ms-settings:camera")]
    [DataRow(WindowsSettingsPage.CameraPrivacy, "ms-settings:privacy-webcam")]
    [DataRow(WindowsSettingsPage.MicrophonePrivacy, "ms-settings:privacy-microphone")]
    public void GetSettingsUriReturnsDocumentedWindowsSettingsUri(WindowsSettingsPage page, string expectedUri)
    {
        Assert.AreEqual(expectedUri, SettingsLauncherService.GetSettingsUri(page));
    }
}
