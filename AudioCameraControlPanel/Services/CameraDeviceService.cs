using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AudioCameraControlPanel.Models;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace AudioCameraControlPanel.Services;

public sealed class CameraDeviceService : ICameraDeviceService
{
    private static readonly TimeSpan MinimumFrameInterval = TimeSpan.FromMilliseconds(66);
    private readonly SemaphoreSlim _previewLock = new(1, 1);
    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _frameReader;
    private int _isProcessingFrame;
    private long _lastPublishedFrameTimestamp;

    public event EventHandler<CameraFrameEventArgs>? FrameReady;

    public event EventHandler<string>? PreviewError;

    public async Task<IReadOnlyList<CameraDeviceInfo>> GetCamerasAsync()
    {
        try
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return devices
                .Select(device => new CameraDeviceInfo(device.Id, device.Name))
                .OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException(
                "Camera permission was denied. Enable camera access for desktop apps in Windows privacy settings.",
                ex);
        }
    }

    public async Task StartPreviewAsync(string cameraDeviceId)
    {
        if (string.IsNullOrWhiteSpace(cameraDeviceId))
        {
            throw new InvalidOperationException("No camera is selected.");
        }

        await _previewLock.WaitAsync();
        try
        {
            await StopPreviewCoreAsync();

            _mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = cameraDeviceId,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            await _mediaCapture.InitializeAsync(settings);

            var source = SelectColorFrameSource(_mediaCapture)
                ?? throw new InvalidOperationException("The selected camera did not expose a color video preview stream.");

            _frameReader = await _mediaCapture.CreateFrameReaderAsync(source, MediaEncodingSubtypes.Bgra8);
            _frameReader.FrameArrived += OnFrameArrived;

            var startStatus = await _frameReader.StartAsync();
            if (startStatus != MediaFrameReaderStartStatus.Success)
            {
                await StopPreviewCoreAsync();
                throw new InvalidOperationException(GetStartStatusMessage(startStatus));
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            await StopPreviewCoreAsync();
            throw new InvalidOperationException(
                "Camera permission was denied. Enable camera access and desktop app camera access in Windows privacy settings.",
                ex);
        }
        catch (COMException ex)
        {
            await StopPreviewCoreAsync();
            throw new InvalidOperationException(GetComCameraErrorMessage(ex), ex);
        }
        finally
        {
            _previewLock.Release();
        }
    }

    public async Task StopPreviewAsync()
    {
        await _previewLock.WaitAsync();
        try
        {
            await StopPreviewCoreAsync();
        }
        finally
        {
            _previewLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopPreviewAsync();
        _previewLock.Dispose();
    }

    private static MediaFrameSource? SelectColorFrameSource(MediaCapture mediaCapture)
    {
        return mediaCapture.FrameSources.Values
            .Where(source => source.Info.SourceKind == MediaFrameSourceKind.Color)
            .OrderBy(source => source.Info.MediaStreamType == MediaStreamType.VideoPreview ? 0 : 1)
            .FirstOrDefault();
    }

    private async Task StopPreviewCoreAsync()
    {
        if (_frameReader is not null)
        {
            _frameReader.FrameArrived -= OnFrameArrived;
            await _frameReader.StopAsync();
            _frameReader.Dispose();
            _frameReader = null;
        }

        _mediaCapture?.Dispose();
        _mediaCapture = null;
        Volatile.Write(ref _isProcessingFrame, 0);
        Interlocked.Exchange(ref _lastPublishedFrameTimestamp, 0);
    }

    private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (!ShouldPublishFrame())
        {
            return;
        }

        if (Interlocked.Exchange(ref _isProcessingFrame, 1) == 1)
        {
            return;
        }

        try
        {
            using var frame = sender.TryAcquireLatestFrame();
            var softwareBitmap = frame?.VideoMediaFrame?.SoftwareBitmap;
            if (softwareBitmap is null)
            {
                return;
            }

            using var convertedBitmap = ConvertToBgraPremultiplied(softwareBitmap);
            var bitmapSource = CreateBitmapSource(convertedBitmap);
            FrameReady?.Invoke(this, new CameraFrameEventArgs(bitmapSource));
        }
        catch (Exception ex) when (ex is ObjectDisposedException or COMException or InvalidOperationException)
        {
            PreviewError?.Invoke(this, $"Unable to read camera preview frames. {ex.Message}");
        }
        finally
        {
            Volatile.Write(ref _isProcessingFrame, 0);
        }
    }

    private bool ShouldPublishFrame()
    {
        var now = Stopwatch.GetTimestamp();
        var previous = Interlocked.Read(ref _lastPublishedFrameTimestamp);
        if (previous != 0 && Stopwatch.GetElapsedTime(previous, now) < MinimumFrameInterval)
        {
            return false;
        }

        Interlocked.Exchange(ref _lastPublishedFrameTimestamp, now);
        return true;
    }

    private static SoftwareBitmap ConvertToBgraPremultiplied(SoftwareBitmap softwareBitmap)
    {
        if (softwareBitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8 &&
            softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Premultiplied)
        {
            return SoftwareBitmap.Copy(softwareBitmap);
        }

        return SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }

    private static BitmapSource CreateBitmapSource(SoftwareBitmap softwareBitmap)
    {
        var width = softwareBitmap.PixelWidth;
        var height = softwareBitmap.PixelHeight;
        var stride = width * 4;
        var bytes = new byte[stride * height];
        var buffer = new Windows.Storage.Streams.Buffer((uint)bytes.Length);
        softwareBitmap.CopyToBuffer(buffer);
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(bytes);

        var bitmapSource = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            bytes,
            stride);
        bitmapSource.Freeze();
        return bitmapSource;
    }

    private static string GetStartStatusMessage(MediaFrameReaderStartStatus status)
    {
        return status switch
        {
            MediaFrameReaderStartStatus.ExclusiveControlNotAvailable =>
                "The selected camera is already in exclusive use by another app.",
            MediaFrameReaderStartStatus.DeviceNotAvailable =>
                "The selected camera is not available.",
            MediaFrameReaderStartStatus.UnknownFailure =>
                "The camera preview failed to start for an unknown device error.",
            _ => $"The camera preview failed to start. Status: {status}."
        };
    }

    private static string GetComCameraErrorMessage(COMException ex)
    {
        return ex.HResult switch
        {
            unchecked((int)0x80070005) =>
                "Camera permission was denied. Check Windows camera privacy settings.",
            unchecked((int)0x80070020) =>
                "The selected camera is busy in another app.",
            unchecked((int)0x80070490) =>
                "The selected camera is unavailable or has been disconnected.",
            _ => $"Unable to start the selected camera. {ex.Message}"
        };
    }
}
