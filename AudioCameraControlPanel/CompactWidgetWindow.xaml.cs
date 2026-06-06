using System.Windows;
using AudioCameraControlPanel.Models;
using AudioCameraControlPanel.ViewModels;

namespace AudioCameraControlPanel;

public partial class CompactWidgetWindow : Window
{
    public CompactWidgetWindow(MainViewModel mainViewModel, CompactWidgetKind kind)
    {
        InitializeComponent();
        DataContext = new CompactWidgetViewModel(mainViewModel, kind);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
