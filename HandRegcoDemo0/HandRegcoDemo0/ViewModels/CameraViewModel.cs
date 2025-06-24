
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
using HandRegcoDemo0.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Printing;
using WinRT;
using System.IO;
using HandRegcoDemo0.NullClass;






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
        private List<HandSign> StoredHandSign;
        public CameraViewModel()
        {
            _imageProcessor = new ImageProcesser();
            _signRecognizer = new SignRecognizer();
            _signRecognizer.LoadDataset("Datasets");
            buttonIsEnable = true;
            cameraCombobox = new ObservableCollection<string>();
            StoredHandSign = new HandSign().PopulateHandSign();
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
                HandSign handSign = getInputHandSign(softwareBitmap);
                Debug.WriteLine(KnnMatch(handSign, StoredHandSign));
                // BitmapImage = DistanceTransformTest(softwareBitmap);
                ProcessedBitmapImage = SoftwareBitmapToImage(softwareBitmap);
                //SkinMaskBitmapImage = DistanceTransformTest(softwareBitmap);
                //BitmapImage = SoftwareBitmapToImage(softwareBitmap);
                softwareBitmap.Dispose();
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

            //handContour = _imageProcessor.PolyLineApprox(handContour);
            if (handContour == null || handContour.Size < 3)
                return _imageProcessor.MatToWriteableBitmap(inputMat); 

            var handConvex = _imageProcessor.GetConvexHull(handContour);
            if (handConvex == null || handConvex.Size < 3)
                return _imageProcessor.MatToWriteableBitmap(inputMat); 

            CvInvoke.DrawContours(inputMat, new VectorOfVectorOfPoint(handContour), -1, new Emgu.CV.Structure.MCvScalar(0, 255, 0), 2);

            Rectangle box = _imageProcessor.getBoundingBox(handContour);
            Segment[] listSegment = new DistanceArithmetic().getSegmentFromHull(handContour, box);

            inputMat = _imageProcessor.DrawSegment(listSegment, inputMat);

            inputMat = _imageProcessor.MarkMinAreaRect(box, inputMat);


            var hullIndices = _imageProcessor.GetConvexHullIndices(handConvex);
            if (hullIndices == null || hullIndices.Size < 3)
                return _imageProcessor.MatToWriteableBitmap(inputMat);

            var defectsMat = _imageProcessor.GetConvexityDefects(handContour);
            if (defectsMat == null || defectsMat.Rows == 0)
                return _imageProcessor.MatToWriteableBitmap(inputMat); // Không có defect nào

            inputMat = _imageProcessor.DrawConvexDefect(inputMat, defectsMat, handContour);

            var recognized = _signRecognizer.Recognize(skinMaskMat);
            RecognizedSign = recognized;

            CvInvoke.PutText(
                inputMat, recognized,
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
            skinMaskMat = _imageProcessor.calculateDistanceTransformation(skinMaskMat);
         //   VectorOfPoint contour = _imageProcessor.FindLargestContour(skinMaskMat);
        //    contour = _imageProcessor.PolyLineApprox(contour);
         //   skinMaskMat = _imageProcessor.DrawContour(contour, inputMat);
            return _imageProcessor.MatToWriteableBitmap(skinMaskMat);
        }
        public string KnnMatch(HandSign inputSign, List<HandSign> database)
        {
            if(inputSign.img is null)
            {
                return "?";
            }
            //double inputSighHullToBoxRatio = new DistanceArithmetic().CalculateHullToBoxRatio(inputSign.convexHull, inputSign.box);
            HandSign output = new HandSign();
            Mat img = inputSign.img.Clone();
            img = _imageProcessor.DetectSkinVer1(img);
            int inputindex = 0;
            using (var contour = _imageProcessor.FindLargestContour(img, out inputindex))
            {
                double shortestDistance = 99999;
                foreach (HandSign sign in database)
                {
                    Mat dbImg = sign.img.Clone();
                    dbImg = _imageProcessor.DetectSkinVer1(dbImg);
                    int dbIndex = 0;
                    using (var checkContour = _imageProcessor.FindLargestContour(dbImg, out dbIndex))
                    {
                        double distance = CvInvoke.MatchShapes(contour[inputindex], checkContour[dbIndex], ContoursMatchType.I1);
                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            output = sign;
                        }
                    }                    
                }
                if (output.img is not null)
                {
                    BitmapImage = _imageProcessor.MatToWriteableBitmap(output.img);
                }

                return output.Word;
            }
        }
        //public System.Drawing.Point[] getSegmentContour(HandSign input)
        //{
        //    List<System.Drawing.Point> inputContourH = new List<System.Drawing.Point>();
        //    List<System.Drawing.Point> inputContourLo = new List<System.Drawing.Point>();
        //    List<System.Drawing.Point> inputContourR = new List<System.Drawing.Point>();
        //    List<System.Drawing.Point> inputContourL = new List<System.Drawing.Point>();
        //    foreach (Segment segment in input.listOfSegment)
        //    {
        //        inputContourH.Add(segment.higestPoint);
        //        inputContourR.Add(segment.rightMostPoint);
        //        inputContourL.Add(segment.leftMostPoint);
        //        inputContourLo.Add(segment.lowestPoint);
        //    }
        //    inputContourH.AddRange(inputContourR);
        //    inputContourH.AddRange(inputContourL);
        //    inputContourH.AddRange(inputContourLo);
 
        //    System.Drawing.Point[] inputContour = inputContourH.ToArray();


        //    return inputContour;
        //}
        private double SegmentDistanceCalculation(System.Drawing.Point[] inputContour, System.Drawing.Point[] dbContour)
        {
            
            double result = 0;
           
            for (int i = 0; i < inputContour.Length; i++)
            {
                result += Math.Pow(inputContour[i].X - dbContour[i].X, 2)
                    + Math.Pow(inputContour[i].Y - dbContour[i].Y, 2);
            }
            return result;
        }
        public double slope(System.Drawing.Point a, System.Drawing.Point b)
        {
            return (a.Y - b.Y) / (a.X - b.X);
        }
        public HandSign getInputHandSign(SoftwareBitmap softwareBitmap)
        {
            Mat img = _imageProcessor.ConvertToMat(softwareBitmap);
            HandSign handSign = new HandSign(img);
            return handSign;
        }
        //double result = 0;
        //DistanceArithmetic DA = new DistanceArithmetic();
        //System.Drawing.Point avgInputHighest = new System.Drawing.Point();
        //System.Drawing.Point avgInputLowest = new System.Drawing.Point();
        //System.Drawing.Point avgInputMiddle = new System.Drawing.Point();
        //System.Drawing.Point avgDbHighest = new System.Drawing.Point();
        //System.Drawing.Point avgDbLowest = new System.Drawing.Point();
        //System.Drawing.Point avgDbMiddle = new System.Drawing.Point();
        //foreach(Segment segment in input.listOfSegment)
        //{
        //    avgInputHighest.X += segment.higestPoint.X / input.listOfSegment.Length;
        //    avgInputHighest.Y += segment.higestPoint.Y / input.listOfSegment.Length;
        //    avgInputLowest.X += segment.lowestPoint.X / input.listOfSegment.Length;
        //    avgInputHighest.Y += segment.lowestPoint.Y / input.listOfSegment.Length;
        //    avgInputMiddle.X += segment.highestMiddlePoint.X / input.listOfSegment.Length;
        //    avgInputMiddle.Y += segment.highestMiddlePoint.Y / input.listOfSegment.Length;
        //}
        //foreach(Segment segment in db.listOfSegment)
        //{
        //    avgDbHighest.X += segment.higestPoint.X / db.listOfSegment.Length;
        //    avgDbHighest.Y += segment.higestPoint.Y / db.listOfSegment.Length;
        //    avgDbLowest.X += segment.lowestPoint.X / db.listOfSegment.Length;
        //    avgDbLowest.Y += segment.lowestPoint.Y / db.listOfSegment.Length;
        //    avgDbMiddle.X += segment.highestMiddlePoint.X / db.listOfSegment.Length;
        //    avgDbMiddle.Y += segment.highestMiddlePoint.Y / db.listOfSegment.Length;
        //}
        //double inputAB = DA.getDistance(avgInputHighest, avgInputMiddle);
        //double inputBC = DA.getDistance(avgInputMiddle, avgInputLowest);
        //double inputCA = DA.getDistance(avgInputLowest, avgInputHighest);
        //double inputAVGAngle = Math.Acos((Math.Pow(inputAB, 2) + Math.Pow(inputBC, 2) - Math.Pow(inputCA, 2))/(2*inputAB*inputBC));

        //double dbAB = DA.getDistance(avgDbHighest, avgDbMiddle);
        //double dbBC = DA.getDistance(avgDbMiddle, avgDbLowest);
        //double dbCA = DA.getDistance(avgDbLowest, avgDbHighest);

        //double dBAVGAngle = Math.Acos((Math.Pow(dbAB, 2) + Math.Pow(dbBC, 2) - Math.Pow(dbCA, 2)) / (2 * dbAB * dbBC));

        //result += Math.Pow(inputAVGAngle - dBAVGAngle, 2);


        ////result += Math.Pow(avgInputHighest.X - avgDbHighest.X, 2)
        ////    + Math.Pow(avgInputHighest.Y - avgDbHighest.Y, 2)
        ////    + Math.Pow(avgInputLowest.X - avgDbLowest.X, 2)
        ////    + Math.Pow(avgInputLowest.Y - avgDbLowest.Y, 2)
        ////    + Math.Pow(avgInputMiddle.X - avgDbMiddle.X, 2)
        ////    + Math.Pow(avgInputMiddle.Y - avgDbMiddle.Y, 2);


        //return result;
    }
}
