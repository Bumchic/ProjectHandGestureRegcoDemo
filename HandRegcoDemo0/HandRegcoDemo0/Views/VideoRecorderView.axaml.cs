using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HandRegcoDemo0.ViewModels;

namespace HandRegcoDemo0;

public partial class VideoRecorderView : UserControl
{
    public VideoRecorderView()
    {
        InitializeComponent();
        DataContext = new VideoRecorderViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
