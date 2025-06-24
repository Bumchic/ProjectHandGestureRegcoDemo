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
using Emgu.CV.CvEnum;
using System.Collections.Generic;




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
        private readonly SignRecognizer _signRecognizer;
        private Queue<string> recentResults = new();
        private const int MaxHistory = 10;
        private const int ConfirmThreshold = 6;
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
        [ObservableProperty]
        private string recognizedSign = "?";
        public CameraViewModel()
        {
            _imageProcessor = new ImageProcesser();
            _signRecognizer = new SignRecognizer();
            _signRecognizer.LoadDataset("Datasets");
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
                BitmapImage = SoftwareBitmapToImage(softwareBitmap);
                ProcessedBitmapImage = ProcessMat(softwareBitmap);
                SkinMaskBitmapImage = DistanceTransformTest(softwareBitmap);
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
        public WriteableBitmap ProcessMat(SoftwareBitmap softwareBitmap)
        {

            var inputMat = _imageProcessor.ConvertToMat(softwareBitmap);
            var processedMat = _imageProcessor.ColorConvertToGray(inputMat);

            var skinMaskMat = _imageProcessor.DetectSkinVer1(inputMat);

            var handContour = _imageProcessor.FindLargestContour(skinMaskMat);
            if (handContour == null || handContour.Size < 3)
                return _imageProcessor.MatToWriteableBitmap(inputMat); 

            handContour = _imageProcessor.PolyLineApprox(handContour);
            if (handContour == null || handContour.Size < 3)
                return _imageProcessor.MatToWriteableBitmap(inputMat); 

            var handConvex = _imageProcessor.GetConvexHull(handContour);
            if (handConvex == null || handConvex.Size < 3)
                return _imageProcessor.MatToWriteableBitmap(inputMat); 

            CvInvoke.DrawContours(inputMat, new VectorOfVectorOfPoint(handConvex), -1, new Emgu.CV.Structure.MCvScalar(0, 255, 0), 2);

            RotatedRect box = CvInvoke.MinAreaRect(handContour);
            inputMat = _imageProcessor.MarkMinAreaRect(box, inputMat);

            var hullIndices = _imageProcessor.GetConvexHullIndices(handConvex);
            if (hullIndices == null || hullIndices.Size < 3)
                return _imageProcessor.MatToWriteableBitmap(inputMat); 

            var defectsMat = _imageProcessor.GetConvexityDefects(handContour);
            if (defectsMat == null || defectsMat.Rows == 0)
                return _imageProcessor.MatToWriteableBitmap(inputMat); 

            inputMat = _imageProcessor.DrawConvexDefect(inputMat, defectsMat, handContour);

            var rawResult = _signRecognizer.Recognize(skinMaskMat);
            var smoothResult = GetStableSign(rawResult);

            CvInvoke.PutText(
                inputMat, smoothResult,
                new System.Drawing.Point(10, 50),
                FontFace.HersheyComplex, 2.0,
                new Emgu.CV.Structure.MCvScalar(255, 0, 0), 3);
            return _imageProcessor.MatToWriteableBitmap(inputMat);
            //ProcessedBitmapImage = _imageProcessor.MatToWriteableBitmap(processedMat);

        }
        public WriteableBitmap DistanceTransformTest(SoftwareBitmap softwareBitmap)
        {
            var inputMat = _imageProcessor.ConvertToMat(softwareBitmap);
            var skinMaskMat = _imageProcessor.DetectSkinVer1(inputMat);
            //skinMaskMat = _imageProcessor.calculateDistanceTransformation(skinMaskMat);
            VectorOfPoint contour = _imageProcessor.FindLargestContour(skinMaskMat);
            contour = _imageProcessor.PolyLineApprox(contour);
            skinMaskMat = _imageProcessor.DrawContour(contour, inputMat);
            return _imageProcessor.MatToWriteableBitmap(skinMaskMat);
        }
        private string GetStableSign(string newResult)
        {
            recentResults.Enqueue(newResult);
            if (recentResults.Count > MaxHistory)
            {
                recentResults.Dequeue();
            }
            var mostCommon = recentResults
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .First();
            if (mostCommon.Count () >= ConfirmThreshold)
                return mostCommon.Key;
            return "?";
        }
    }
}
