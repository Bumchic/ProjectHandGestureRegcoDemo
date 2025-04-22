using Avalonia.Controls;
using Windows.Devices.Enumeration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Avalonia.Interactivity;
using System.Linq;
using Windows.Security.Authorization.AppCapabilityAccess;


namespace HandRegcoDemo0.Views;

public partial class MainWindow : Window
{
    public VideoDeviceController media { get; set; }
    public MediaCapture mediaCapture { get; set; }
    public DeviceInformationCollection devices { get; set; }
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            InitCapMedia();
            AddCameraOption();
        }catch(Exception e)
        {
            throw new Exception(e.Message);
        }
        
        
    }
    public async Task InitCapMedia()
    {
        mediaCapture = new MediaCapture();
        await mediaCapture.InitializeAsync();
    }
    public async Task AddCameraOption()
    {
        devices = await DeviceInformation.FindAllAsync(MediaDevice.GetVideoCaptureSelector());
        foreach(DeviceInformation item in devices)
        {
            DeviceChoices.Items.Add(item.Name);
        }
    }
    public void StartCamOnClick(object? sender, RoutedEventArgs args)
    {
        
        DeviceInformation Camera = devices.First(a => a.Name.Equals(DeviceChoices.SelectedItem));
        if(mediaCapture == null)
        {
            throw new Exception("media Capture is null");
            
        }
        
        if(AppCapability.Create("WebCam").CheckAccess() != AppCapabilityAccessStatus.Allowed)
        {
            throw new Exception("WebCam Access Denied");
            
        }
    }

}