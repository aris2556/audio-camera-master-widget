using AudioCameraControlPanel.Models;

namespace AudioCameraControlPanel.Services;

public interface ICameraDeviceService : IAsyncDisposable
{
    event EventHandler<CameraFrameEventArgs>? FrameReady;

    event EventHandler<string>? PreviewError;

    Task<IReadOnlyList<CameraDeviceInfo>> GetCamerasAsync();

    Task StartPreviewAsync(string cameraDeviceId);

    Task StopPreviewAsync();
}
