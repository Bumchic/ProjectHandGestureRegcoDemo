using Avalonia.Controls;
using Windows.Devices.Enumeration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Avalonia.Interactivity;


namespace HandRegcoDemo0.Views;

public partial class MainWindow : Window
{
    public VideoDeviceController media { get; set; }
    public MediaCapture mediaCapture { get; set; }
    public MainWindow()
    {
        InitializeComponent();
        InitCapMedia();
        AddCameraOption();
        
    }
    public async Task InitCapMedia()
    {
        mediaCapture = new MediaCapture();
        await mediaCapture.InitializeAsync();
    }
    public async Task AddCameraOption()
    {
        DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(MediaDevice.GetVideoCaptureSelector());
        Debug.WriteLine(devices.Count);
        foreach(DeviceInformation item in devices)
        {
            DeviceChoices.Items.Add(item.Name);
        }
    }
    public void StartCamOnClick(object? sender, RoutedEventArgs args)
    {
        Debug.WriteLine("a");
    }

}