using System.Windows.Media.Imaging;

namespace AudioCameraControlPanel.Services;

public sealed class CameraFrameEventArgs : EventArgs
{
    public CameraFrameEventArgs(BitmapSource frame)
    {
        Frame = frame;
    }

    public BitmapSource Frame { get; }
}
