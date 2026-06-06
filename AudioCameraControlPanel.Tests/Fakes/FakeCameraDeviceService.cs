using System.Windows.Media.Imaging;
using AudioCameraControlPanel.Models;
using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.Tests.Fakes;

public sealed class FakeCameraDeviceService : ICameraDeviceService
{
    public event EventHandler<CameraFrameEventArgs>? FrameReady;

    public event EventHandler<string>? PreviewError;

    public List<CameraDeviceInfo> Cameras { get; } = new();

    public List<string> StartPreviewCalls { get; } = new();

    public List<PreviewLifecycleCall> PreviewLifecycleCalls { get; } = new();

    public int StopPreviewCount { get; private set; }

    public bool IsPreviewRunning { get; private set; }

    public string? PreviewCameraDeviceId { get; private set; }

    public bool IsDisposed { get; private set; }

    public Exception? GetCamerasException { get; set; }

    public Exception? StartPreviewException { get; set; }

    public Exception? StopPreviewException { get; set; }

    public Task<IReadOnlyList<CameraDeviceInfo>> GetCamerasAsync()
    {
        return GetCamerasException is null
            ? Task.FromResult<IReadOnlyList<CameraDeviceInfo>>(Cameras.ToList())
            : Task.FromException<IReadOnlyList<CameraDeviceInfo>>(GetCamerasException);
    }

    public Task StartPreviewAsync(string cameraDeviceId)
    {
        StartPreviewCalls.Add(cameraDeviceId);
        PreviewLifecycleCalls.Add(new PreviewLifecycleCall("Start", cameraDeviceId));

        if (StartPreviewException is not null)
        {
            return Task.FromException(StartPreviewException);
        }

        IsPreviewRunning = true;
        PreviewCameraDeviceId = cameraDeviceId;
        return Task.CompletedTask;
    }

    public Task StopPreviewAsync()
    {
        StopPreviewCount++;
        PreviewLifecycleCalls.Add(new PreviewLifecycleCall("Stop", PreviewCameraDeviceId));

        if (StopPreviewException is not null)
        {
            return Task.FromException(StopPreviewException);
        }

        IsPreviewRunning = false;
        PreviewCameraDeviceId = null;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        IsDisposed = true;
        await StopPreviewAsync();
    }

    public CameraDeviceInfo AddCamera(string id, string name)
    {
        var camera = new CameraDeviceInfo(id, name);
        Cameras.Add(camera);
        return camera;
    }

    public void RaiseFrameReady(BitmapSource frame)
    {
        FrameReady?.Invoke(this, new CameraFrameEventArgs(frame));
    }

    public void RaisePreviewError(string message)
    {
        PreviewError?.Invoke(this, message);
    }

    public readonly record struct PreviewLifecycleCall(string Action, string? CameraDeviceId);
}
