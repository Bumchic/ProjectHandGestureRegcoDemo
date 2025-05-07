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



namespace HandRegcoDemo0.Views;

public partial class MainWindow : Window
{
    public VideoDeviceController media { get; set; }
    public MediaCapture mediaCapture { get; set; }
    public DeviceInformationCollection devices { get; set; }
    public MediaFrameSource frameSource { get; set; }
    public MediaPlayer mediaPlayer { get; set; }
    public MediaFrameReader mediaFrameReader { get; set; }
    public Dispatcher dispatcher { get; set; }
    public Avalonia.Media.Imaging.WriteableBitmap image { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            AddCameraOption();
            dispatcher = Dispatcher.UIThread;
            
        }catch(Exception e)
        {
            throw new Exception(e.Message);
        }
        
        
    }
    public async Task InitCapMedia(MediaCaptureInitializationSettings settings)
    {
        try
        {
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(settings);
        }catch(Exception)
        {
            throw new Exception();
        }
        
    }
    public async Task AddCameraOption()
    {
        devices = await DeviceInformation.FindAllAsync(MediaDevice.GetVideoCaptureSelector());
        foreach(DeviceInformation item in devices)
        {
            DeviceChoices.Items.Add(item.Name);
        }
    }
    public async void StartCamOnClick(object? sender, RoutedEventArgs args)
    {
        MediaFrameSource previewSource;
        MediaFrameSource recordSource;
        DeviceInformation Camera = devices.First(a => a.Name.Equals(DeviceChoices.SelectedItem));
        if (mediaCapture != null)
        {
            throw new Exception("media Capture is not null");

        }

        if (AppCapability.Create("WebCam").CheckAccess() != AppCapabilityAccessStatus.Allowed)
        {
            throw new Exception("WebCam Access Denied");

        }

        try
        {
            MediaCaptureInitializationSettings settings;
            settings = new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = Camera.Id,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };
            await InitCapMedia(settings);
            Debug.WriteLine("Success");

        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }

        frameSource = null;
        try
        {
            previewSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoPreview && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
        }
        catch (Exception)
        {
            return;
        }

        if (previewSource != null)
        {
            frameSource = previewSource;
        }
        else
        {
            recordSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord
                                                                                   && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
            frameSource = recordSource;
        }
        if (frameSource == null)
        {
            throw new Exception();
        }
        mediaPlayer = new MediaPlayer()
        {
            RealTimePlayback = true,
            AutoPlay = false,
            Source = MediaSource.CreateFromMediaFrameSource(frameSource)
        };

        mediaPlayer.MediaFailed += OnMediaFailed;
        mediaPlayer.MediaOpened += OnMediaOpened;

    }
    public void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        throw new Exception(args.ErrorMessage);
    }
    public async void OnMediaOpened(MediaPlayer sender, object? a)
    {
        Debug.WriteLine("MediaOpened");
        mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(frameSource);
        mediaFrameReader.FrameArrived += onFrameArrived;
        await mediaFrameReader.StartAsync();
    }
    public void onFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        MediaFrameReference mediaFrameReference = sender.TryAcquireLatestFrame();
        VideoMediaFrame videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
        SoftwareBitmap softwareBitmap = videoMediaFrame?.SoftwareBitmap;
        if (softwareBitmap != null)
        {
            
            if(softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }
            SoftwareBitmapToImage(softwareBitmap);
        }
    }
    public unsafe void SoftwareBitmapToImage(SoftwareBitmap softwareBitmap)
    {
        PixelFormat pixelFormat = PixelFormat.Bgra8888;
        AlphaFormat alphaFormat = AlphaFormat.Premul;
        PixelSize pixelSize = new PixelSize(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
        Vector dpi = new Vector(softwareBitmap.DpiX, softwareBitmap.DpiY);
        int stride = ((softwareBitmap.PixelWidth * 32 + 31) & ~31) / 8;
        Buffer buffer = new Buffer((uint)(4 * softwareBitmap.PixelWidth * softwareBitmap.PixelHeight));
        byte[] bytes = new byte[4 * softwareBitmap.PixelWidth * softwareBitmap.PixelHeight];
        softwareBitmap.CopyToBuffer(bytes.AsBuffer());
        fixed (byte* p = bytes)
        {
            IntPtr intptr = (IntPtr)p;
            Avalonia.Media.Imaging.WriteableBitmap bitmap = new Avalonia.Media.Imaging.WriteableBitmap(pixelFormat, alphaFormat, intptr, pixelSize, dpi, stride);
            dispatcher.Invoke(() =>
            {
                IFrameReaderImageControl.Source = bitmap;
            });
        }
    }
}