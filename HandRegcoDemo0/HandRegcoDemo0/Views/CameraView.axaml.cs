using Avalonia.Controls;
using HandRegcoDemo0.ViewModels;
using Avalonia.Interactivity;



namespace HandRegcoDemo0.Views;

public partial class CameraView : Window
{
    private CameraViewModel cameraViewModel;
    public CameraView()
    {
        InitializeComponent();
        cameraViewModel = (CameraViewModel)DataContext;
    }
    public async void StartCamOnClick(object? sender, RoutedEventArgs args)
    {
        cameraViewModel.StartCamOnClick(sender, args);
    }
}