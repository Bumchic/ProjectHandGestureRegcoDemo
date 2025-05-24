using Avalonia.Controls;
using Windows.Devices.Enumeration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Devices;
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
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;




namespace HandRegcoDemo0.ViewModels
{
    partial class CameraViewModel : ViewModelBase
    {
        private VideoDeviceController media;
        private MediaCapture mediaCapture;
        private DeviceInformationCollection devices;
        private MediaFrameSource frameSource;
        private MediaPlayer mediaPlayer;
        private MediaFrameReader mediaFrameReader;
        [ObservableProperty]
        public Avalonia.Media.Imaging.WriteableBitmap bitmapImage;
        [ObservableProperty]
        public Avalonia.Media.Imaging.WriteableBitmap processedBitmapImage;
        //public ComboBox cameraCombobox { get; set; }
        public ObservableCollection<string> cameraCombobox { get; set; }
        public int SelectedIndex { get; set; }
        private readonly ImageProcesser _imageProcessor = new ImageProcesser();
        public CameraViewModel()
        {
            cameraCombobox = new ObservableCollection<string>();
            AddCameraOption();
        }
        public async Task InitCapMedia(MediaCaptureInitializationSettings settings)
        {
            try
            {
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(settings);
            }
            catch (Exception)
            {
                throw new Exception();
            }

        }
        public async Task AddCameraOption()
        {
            devices = await DeviceInformation.FindAllAsync(MediaDevice.GetVideoCaptureSelector());
            foreach (DeviceInformation item in devices)
            {
                cameraCombobox.Add(item.Name);
            }
        }
        public async void StartCamOnClick()
        {
            MediaFrameSource previewSource;
            MediaFrameSource recordSource;
            DeviceInformation Camera = devices.First(a => a.Name.Equals(cameraCombobox[SelectedIndex]));
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

                if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }
                BitmapImage = SoftwareBitmapToImage(softwareBitmap);

                var inputMat = ImageProcesser.ConvertToMat(softwareBitmap);
                var processedMat = _imageProcessor.ProcessGesture(inputMat);
                ProcessedBitmapImage = _imageProcessor.MatToWriteableBitmap(processedMat);
            }
        }
        public unsafe Avalonia.Media.Imaging.WriteableBitmap SoftwareBitmapToImage(SoftwareBitmap softwareBitmap)
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
                return bitmap;
            }
        }
    }
}
