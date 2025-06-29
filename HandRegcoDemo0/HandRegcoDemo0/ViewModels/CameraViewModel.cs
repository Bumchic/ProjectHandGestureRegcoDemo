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
using HandRegcoDemo0.Utils.Segmentation;
using Windows.Storage.Pickers;
using System.Collections.Generic;
using System.IO;




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
        [ObservableProperty]
        private string recognizedSign = "?";
        public Mat a;
        public SoftwareBitmap softwareBitmap ;
        private Mat[] matCollection;
        private int index
        {
            get
            {
                return index;
            }
            set
            {
                if(index == matCollection.Length)
                {
                    index = 0;
                }
                if(index < 0)
                {
                    index = matCollection.Length - 1;
                }
            }
        }
        public CameraViewModel()
        {
            a = CvInvoke.Imread("Datasets/B.jpg", ImreadModes.Color);
            CvInvoke.CvtColor(a, a, ColorConversion.Bgr2Bgra);
            matCollection = getALlPic();
            softwareBitmap = new ImageConverter().ConvertMatToSoftwareBitmap(a);
            _imageProcessor = new ImageProcesser();
            buttonIsEnable = true;
            cameraCombobox = new ObservableCollection<string>();
            AddCameraOption();
        }
        public void NextPic()
        {
            index++;
            string b;
            SoftwareBitmap a = new ImageConverter().ConvertMatToSoftwareBitmap(matCollection[index]);
            ProcessedBitmapImage = _imageProcessor.ProcessMat(a,out b);
        }
        public Mat[] getALlPic()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo("~~/Datasets");
            FileInfo[] fileInfo = directoryInfo.GetFiles();
            List<Mat> matCollection = new List<Mat>();
            foreach(var file in fileInfo)
            {
                if(file.Extension.Equals(".jpg"))
                {
                    Mat img = new Mat();
                    byte[] bytes = File.ReadAllBytes(file.DirectoryName);
                    CvInvoke.Imdecode(bytes, ImreadModes.Color, img);
                    CvInvoke.CvtColor(img, img, ColorConversion.Bgr2Bgra);
                    matCollection.Add(img);
                }
            }
            return matCollection.ToArray();
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
            //MediaFrameSource previewSource;
            //MediaFrameSource recordSource;
            //DeviceInformation Camera = devices.First(a => a.Name.Equals(cameraCombobox[SelectedIndex]));
            //if (mediaCapture != null)
            //{
            //    return;

            //}

            //if (AppCapability.Create("WebCam").CheckAccess() != AppCapabilityAccessStatus.Allowed)
            //{
            //    throw new Exception("WebCam Access Denied");

            //}

            //MediaCaptureInitializationSettings settings;
            //settings = new MediaCaptureInitializationSettings()
            //{
            //    VideoDeviceId = Camera.Id,
            //    StreamingCaptureMode = StreamingCaptureMode.Video,
            //    MemoryPreference = MediaCaptureMemoryPreference.Cpu
            //};
            //await InitCapMedia(settings);
            //Debug.WriteLine("Success");


            //frameSource = null;
            //previewSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoPreview && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
            //if (previewSource != null)
            //{
            //    frameSource = previewSource;
            //}
            //else
            //{
            //    recordSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord
            //                                                                           && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
            //    frameSource = recordSource;
            //}
            //ButtonIsEnable = false;
            //mediaPlayer = new MediaPlayer()
            //{
            //    RealTimePlayback = true,
            //    AutoPlay = false,
            //    Source = MediaSource.CreateFromMediaFrameSource(frameSource)
            //};

            //mediaPlayer.MediaFailed += OnMediaFailed;
            //mediaPlayer.MediaOpened += OnMediaOpened;

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
            //SoftwareBitmap softwareBitmap = videoMediaFrame?.SoftwareBitmap;


            string word = "?";
            if (softwareBitmap != null)
            {

                if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }
                //BitmapImage = SoftwareBitmapToImage(softwareBitmap);
                //ProcessedBitmapImage = _imageProcessor.ProcessMat(softwareBitmap, out word);
                //RecognizedSign = word;

                //SkinMaskBitmapImage = DistanceTransformTest(softwareBitmap);
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

        //public WriteableBitmap DistanceTransformTest(SoftwareBitmap softwareBitmap)
        //{
        //    var inputMat = _imageProcessor.ConvertToMat(softwareBitmap);
        //    var skinMaskMat = _imageProcessor.DetectSkinVer1(inputMat);
        //    //skinMaskMat = _imageProcessor.calculateDistanceTransformation(skinMaskMat);
        //    VectorOfPoint contour = _imageProcessor.FindLargestContour(skinMaskMat);
        //    contour = _imageProcessor.PolyLineApprox(contour);
        //    skinMaskMat = _imageProcessor.DrawContour(contour, inputMat);
        //    return _imageProcessor.MatToWriteableBitmap(skinMaskMat);
        //}
    }
}
