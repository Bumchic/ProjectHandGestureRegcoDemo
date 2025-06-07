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
using Avalonia.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.CV.Cuda;




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
        private readonly ImageProcesser _imageProcessor;
        [ObservableProperty]
        public Avalonia.Media.Imaging.WriteableBitmap bitmapImage;
        [ObservableProperty]
        public Avalonia.Media.Imaging.WriteableBitmap processedBitmapImage;
        [ObservableProperty]
        public Avalonia.Media.Imaging.WriteableBitmap skinMaskBitmapImage;

        public ObservableCollection<string> cameraCombobox { get; set; }
        [ObservableProperty]
        private bool buttonIsEnable;
        public int SelectedIndex { get; set; }
        
        public CameraViewModel()
        {
            _imageProcessor = new ImageProcesser();
            buttonIsEnable = true;
            cameraCombobox = new ObservableCollection<string>();
            AddCameraOption();
        }
        public async Task InitCapMedia(MediaCaptureInitializationSettings settings)
        {
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(settings);
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
                return;

            }

            if (AppCapability.Create("WebCam").CheckAccess() != AppCapabilityAccessStatus.Allowed)
            {
                throw new Exception("WebCam Access Denied");

            }

                MediaCaptureInitializationSettings settings;
                settings = new MediaCaptureInitializationSettings()
                {
                    VideoDeviceId = Camera.Id,
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu
                };
                await InitCapMedia(settings);
                Debug.WriteLine("Success");


            frameSource = null;
                previewSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoPreview && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
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
            ButtonIsEnable = false;
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
                BitmapImage = _imageProcessor.SoftwareBitmapToImage(softwareBitmap);

                var inputMat = _imageProcessor.ConvertToMat(softwareBitmap);

                var processedMat = _imageProcessor.ProcessGesture(inputMat);
                var skinMaskMat = _imageProcessor.DetectSkinVer1(inputMat);
                var handContour = _imageProcessor.FindLargestContour(skinMaskMat);
                handContour = _imageProcessor.GetConvexHull(handContour);
                if (handContour != null)
                {
                    CvInvoke.DrawContours(inputMat, new VectorOfVectorOfPoint(handContour), -1, new Emgu.CV.Structure.MCvScalar(0, 255, 0), 2);
                    RotatedRect box = CvInvoke.MinAreaRect(handContour);
                    inputMat = _imageProcessor.MarkFingerPoint(handContour, inputMat);
                    inputMat = _imageProcessor.MarkMinAreaRect(box, inputMat);
                }


                //ProcessedBitmapImage = _imageProcessor.MatToWriteableBitmap(processedMat);

                ProcessedBitmapImage = _imageProcessor.MatToWriteableBitmap(inputMat);
                SkinMaskBitmapImage = _imageProcessor.MatToWriteableBitmap(skinMaskMat);
            }
        }
        

    }
}
