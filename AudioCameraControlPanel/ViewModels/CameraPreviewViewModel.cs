using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using AudioCameraControlPanel.Models;
using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.ViewModels;

public sealed class CameraPreviewViewModel : ObservableObject, IAsyncDisposable
{
    private readonly ICameraDeviceService _cameraDeviceService;
    private readonly SemaphoreSlim _previewOperationLock = new(1, 1);
    private CameraDeviceInfo? _selectedCamera;
    private bool _isPreviewRunning;
    private BitmapSource? _frame;
    private string _statusText = "Camera devices have not been loaded.";
    private int _selectionVersion;

    public CameraPreviewViewModel(ICameraDeviceService cameraDeviceService)
    {
        _cameraDeviceService = cameraDeviceService;

        RefreshCamerasCommand = new AsyncRelayCommand(() => RefreshCamerasAsync(null));
        TogglePreviewCommand = new AsyncRelayCommand(TogglePreviewAsync, () => SelectedCamera is not null);

        _cameraDeviceService.FrameReady += OnFrameReady;
        _cameraDeviceService.PreviewError += OnPreviewError;
    }

    public event EventHandler? SelectedCameraChanged;

    public ObservableCollection<CameraDeviceInfo> Cameras { get; } = new();

    public AsyncRelayCommand RefreshCamerasCommand { get; }

    public AsyncRelayCommand TogglePreviewCommand { get; }

    public CameraDeviceInfo? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (SetProperty(ref _selectedCamera, value))
            {
                var selectionVersion = Interlocked.Increment(ref _selectionVersion);
                SelectedCameraChanged?.Invoke(this, EventArgs.Empty);
                TogglePreviewCommand.RaiseCanExecuteChanged();

                if (IsPreviewRunning)
                {
                    _ = RestartPreviewAsync(selectionVersion);
                }
            }
        }
    }

    public bool IsPreviewRunning
    {
        get => _isPreviewRunning;
        private set
        {
            if (SetProperty(ref _isPreviewRunning, value))
            {
                OnPropertyChanged(nameof(PreviewButtonText));
                OnPropertyChanged(nameof(PreviewStateText));
                OnPropertyChanged(nameof(IsPreviewStopped));
            }
        }
    }

    public bool IsPreviewStopped => !IsPreviewRunning;

    public BitmapSource? Frame
    {
        get => _frame;
        private set => SetProperty(ref _frame, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string PreviewButtonText => IsPreviewRunning ? "Stop preview" : "Start preview";

    public string PreviewStateText => IsPreviewRunning ? "Running" : "Stopped";

    public async Task RefreshCamerasAsync(string? preferredCameraId)
    {
        try
        {
            if (IsPreviewRunning)
            {
                await StopPreviewAsync();
            }

            var devices = await _cameraDeviceService.GetCamerasAsync();
            Replace(Cameras, devices);
            SelectedCamera = PickPreferred(devices, preferredCameraId);
            StatusText = devices.Count == 0
                ? "No cameras were found."
                : $"{devices.Count} camera(s) available.";
        }
        catch (Exception ex)
        {
            Cameras.Clear();
            SelectedCamera = null;
            StatusText = $"Unable to enumerate cameras. {ex.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopPreviewAsync();
        _cameraDeviceService.FrameReady -= OnFrameReady;
        _cameraDeviceService.PreviewError -= OnPreviewError;
        await _cameraDeviceService.DisposeAsync();
        _previewOperationLock.Dispose();
    }

    public void SetStatusText(string statusText)
    {
        StatusText = statusText;
    }

    private async Task TogglePreviewAsync()
    {
        await _previewOperationLock.WaitAsync();
        try
        {
            if (IsPreviewRunning)
            {
                await StopPreviewCoreAsync();
            }
            else
            {
                await StartPreviewCoreAsync();
            }
        }
        finally
        {
            _previewOperationLock.Release();
        }
    }

    private async Task RestartPreviewAsync(int selectionVersion)
    {
        await _previewOperationLock.WaitAsync();
        try
        {
            if (selectionVersion != Volatile.Read(ref _selectionVersion))
            {
                return;
            }

            await StopPreviewCoreAsync();

            if (selectionVersion == Volatile.Read(ref _selectionVersion))
            {
                await StartPreviewCoreAsync();
            }
        }
        catch (Exception ex)
        {
            IsPreviewRunning = false;
            Frame = null;
            StatusText = ex.Message;
        }
        finally
        {
            _previewOperationLock.Release();
        }
    }

    private async Task StartPreviewAsync()
    {
        await _previewOperationLock.WaitAsync();
        try
        {
            await StartPreviewCoreAsync();
        }
        finally
        {
            _previewOperationLock.Release();
        }
    }

    private async Task StopPreviewAsync()
    {
        await _previewOperationLock.WaitAsync();
        try
        {
            await StopPreviewCoreAsync();
        }
        finally
        {
            _previewOperationLock.Release();
        }
    }

    private async Task StartPreviewCoreAsync()
    {
        if (SelectedCamera is null)
        {
            StatusText = "Select a camera before starting preview.";
            return;
        }

        try
        {
            StatusText = "Starting camera preview...";
            await _cameraDeviceService.StartPreviewAsync(SelectedCamera.Id);
            IsPreviewRunning = true;
            StatusText = "Camera preview is running. No video is recorded or saved.";
        }
        catch (Exception ex)
        {
            IsPreviewRunning = false;
            Frame = null;
            StatusText = ex.Message;
        }
    }

    private async Task StopPreviewCoreAsync()
    {
        try
        {
            if (IsPreviewRunning)
            {
                await _cameraDeviceService.StopPreviewAsync();
            }

            IsPreviewRunning = false;
            Frame = null;
            StatusText = "Camera preview stopped. The physical device was not disabled globally.";
        }
        catch (Exception ex)
        {
            IsPreviewRunning = false;
            Frame = null;
            StatusText = $"Unable to stop camera preview. {ex.Message}";
        }
    }

    private void OnFrameReady(object? sender, CameraFrameEventArgs e)
    {
        void ApplyFrame()
        {
            Frame = e.Frame;
        }

        RunOnApplicationDispatcher(ApplyFrame);
    }

    private void OnPreviewError(object? sender, string message)
    {
        _ = RunOnApplicationDispatcherAsync(() => ApplyPreviewErrorAsync(message));
    }

    private async Task ApplyPreviewErrorAsync(string message)
    {
        await _previewOperationLock.WaitAsync();
        try
        {
            try
            {
                await _cameraDeviceService.StopPreviewAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
            {
            }

            StatusText = message;
            IsPreviewRunning = false;
            Frame = null;
        }
        finally
        {
            _previewOperationLock.Release();
        }
    }

    private static void RunOnApplicationDispatcher(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.BeginInvoke(action);
    }

    private static Task RunOnApplicationDispatcherAsync(Func<Task> action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            return action();
        }

        return dispatcher.InvokeAsync(action).Task.Unwrap();
    }

    private static CameraDeviceInfo? PickPreferred(IReadOnlyList<CameraDeviceInfo> devices, string? savedId)
    {
        if (devices.Count == 0)
        {
            return null;
        }

        return devices.FirstOrDefault(device => device.Id == savedId) ?? devices[0];
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }
}
