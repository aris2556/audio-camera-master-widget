using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AudioCameraControlPanel.Controls;

public partial class CameraPreviewPane : UserControl
{
    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame),
        typeof(ImageSource),
        typeof(CameraPreviewPane),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsStoppedProperty = DependencyProperty.Register(
        nameof(IsStopped),
        typeof(bool),
        typeof(CameraPreviewPane),
        new PropertyMetadata(true));

    public static readonly DependencyProperty PreviewMinHeightProperty = DependencyProperty.Register(
        nameof(PreviewMinHeight),
        typeof(double),
        typeof(CameraPreviewPane),
        new PropertyMetadata(170d));

    public CameraPreviewPane()
    {
        InitializeComponent();
    }

    public ImageSource? Frame
    {
        get => (ImageSource?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public bool IsStopped
    {
        get => (bool)GetValue(IsStoppedProperty);
        set => SetValue(IsStoppedProperty, value);
    }

    public double PreviewMinHeight
    {
        get => (double)GetValue(PreviewMinHeightProperty);
        set => SetValue(PreviewMinHeightProperty, value);
    }
}
