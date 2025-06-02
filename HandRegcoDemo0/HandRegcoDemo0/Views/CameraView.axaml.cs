using Avalonia.Controls;
using Windows.Devices.Enumeration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Avalonia.Interactivity;
using System.Linq;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.Media.Capture.Frames;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Graphics.Imaging;
using Avalonia.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using Buffer = Windows.Storage.Streams.Buffer;
using Avalonia.Platform;
using Avalonia;
using HandRegcoDemo0.ViewModels;
using Emgu.CV;


namespace HandRegcoDemo0.Views;

public partial class CameraView : Window
{
   

    public CameraView()
    {
        InitializeComponent();
       
    }
   

}