using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Windows.Graphics.Imaging;
using Emgu.CV.Structure;
using Emgu.CV.Reg;
using Emgu.CV.Util;
using System.Drawing;
using Point = System.Drawing.Point;


namespace HandRegcoDemo0.ViewModels
{
    public partial class ImageProcesser
    {
        private readonly Rgba red = new Rgba(0, 0, 255, 366);
        private readonly Rgba green = new Rgba(0, 255, 0, 366);
        public struct ConvexityDefect
        {
            public Point StartPoint;
            public Point EndPoint;
            public Point DepthPoint;
            public float Depth;
        }

        public Mat ConvertToMat(SoftwareBitmap softwareBitmap)
        {
            if(softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {   
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);
            }
            byte[] data = new byte[4 * softwareBitmap.PixelWidth * softwareBitmap.PixelHeight];
            softwareBitmap.CopyToBuffer(data.AsBuffer());

            Mat mat = new Mat(softwareBitmap.PixelHeight, softwareBitmap.PixelWidth, DepthType.Cv8U, 4);
            mat.SetTo(data);
            return mat;
        }

        public Mat ColorConvertToGray(Mat inputMat)
        {
            Mat gray = new Mat();
            CvInvoke.CvtColor(inputMat, gray, ColorConversion.Bgra2Gray);
            return gray;
        }


        //Convert mat to SB
        public SoftwareBitmap ConvertMatToSoftwareBitmap(Mat mat)
        {
            //if (mat.NumberOfChannels != 4)
            //{F
            //    Mat converted = new Mat();
            //    CvInvoke.CvtColor(mat, converted, ColorConversion.Gray2Bgra);
            //    mat = converted;
            //}
            byte[] data = new byte[mat.Rows * mat.Cols * mat.NumberOfChannels];
            mat.CopyTo(data);
            var bitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, mat.Cols, mat.Rows);
            bitmap.CopyFromBuffer(data.AsBuffer());
            return bitmap;
        }

        public Mat DetectSkinVer1(Mat inputMat)
        {
            Mat yCrcb = new Mat();
            CvInvoke.CvtColor(inputMat, yCrcb, ColorConversion.Bgr2YCrCb);

            Mat skinMask = new Mat();
            CvInvoke.InRange(yCrcb, new ScalarArray(new MCvScalar(0, 133, 77)), new ScalarArray(new MCvScalar(255, 173, 127)), skinMask);
            CvInvoke.GaussianBlur(skinMask, skinMask, new System.Drawing.Size(5, 5), 0);

            return skinMask;
        }

        public VectorOfPoint FindLargestContour(Mat skinMask)
        {
            var countours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(skinMask, countours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            double maxArea = 0;
            VectorOfPoint largestContour = null;

            for (int i = 0; i < countours.Size; i++)
            {
                double area = CvInvoke.ContourArea(countours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = countours[i];
                }
            }
            return largestContour;
        }

        public WriteableBitmap MatToWriteableBitmap(Mat mat)
        {
            //if (mat.NumberOfChannels != 4)
            //{
            //    Mat converted = new Mat();
            //    CvInvoke.CvtColor(mat, converted, ColorConversion.Gray2Bgra);
            //    mat = converted;
            //}

            int width = mat.Cols;
            int height = mat.Rows;
            int stride = width * 4;
            byte[] bytes = new byte[4*height * stride];
            mat.CopyTo(bytes);
            unsafe
            {
                fixed (byte* pBytes = bytes)
                {
                    return new WriteableBitmap(
                        PixelFormat.Bgra8888,
                        AlphaFormat.Premul,
                        (IntPtr)pBytes,
                        new PixelSize(width, height),
                        new Vector(96, 96),
                        stride);
                }
            }
        }
        public unsafe Avalonia.Media.Imaging.WriteableBitmap SoftwareBitmapToImage(SoftwareBitmap softwareBitmap)
        {
            PixelFormat pixelFormat = PixelFormat.Bgra8888;
            AlphaFormat alphaFormat = AlphaFormat.Premul;
            PixelSize pixelSize = new PixelSize(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
            Vector dpi = new Vector(softwareBitmap.DpiX, softwareBitmap.DpiY);
            int stride = ((softwareBitmap.PixelWidth * 32 + 31) & ~31) / 8;
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
    partial class ImageProcesser
    {
        //Minh
        public VectorOfPoint GetConvexHull(VectorOfPoint contour)
        {
            //PointF[] points = new PointF[contour.Size];
            //for(int i=0; i<contour.Size; i++)
            //{
            //    points[i] = contour[i];
            //}
            VectorOfPoint hull = new VectorOfPoint(contour.Size);

            CvInvoke.ConvexHull(contour, hull, false, false);
    
            
            return hull;
        }


        public Mat MarkConvexHullPoints(VectorOfPoint hull, Mat image)
        {
            if (hull == null || image == null)
                return image;
            for (int i=0; i<hull.Size; i++)
            {
                CvInvoke.DrawMarker(image, hull[i], red.MCvScalar, MarkerTypes.Cross, 30, 10);
            }
            return image;
        }
        public Mat MarkMinAreaRect(RotatedRect box, Mat image)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            foreach(PointF pointf in box.GetVertices())
            {
                points.Add(System.Drawing.Point.Round(pointf));
            }
            VectorOfPoint vector = new VectorOfPoint(values: points.ToArray());
            CvInvoke.Polylines(image, vector, true, red.MCvScalar, 2);
            return image;
        }
        public Mat MarkFingerPoint(VectorOfPoint hull, Mat image)
        {
            if(hull == null)
            {
                return image;
            }
            List<System.Drawing.Point> fingerpoints = new List<System.Drawing.Point>();
            RotatedRect box = CvInvoke.MinAreaRect(hull);
            System.Drawing.Point CheckPoint = hull[0];
            for(int i=1; i<hull.Size; i++)
            {
                if(getDistance(CheckPoint, hull[i]) > box.Size.Width / 10)
                {
                    fingerpoints.Add(CheckPoint);
                }
                CheckPoint = hull[i];
            }
            VectorOfPoint fingers = new VectorOfPoint(fingerpoints.Count);
            fingers.Push(fingerpoints.ToArray());
            image = MarkConvexHullPoints(fingers, image);
            return image;
        }
        public double getDistance(System.Drawing.Point a, System.Drawing.Point b)
        {
            double result = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            return result;
        }
        public VectorOfPoint FindLargestContour(VectorOfVectorOfPoint contours)
        {
            if(contours == null)
            {
                return null;
            }
            double maxArea = 0;
            VectorOfPoint largestContour = null;

            for (int i = 0; i < contours.Size; i++)
            {
                double area = CvInvoke.ContourArea(contours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = contours[i];
                }
            }


            return largestContour;
        }
        public VectorOfInt GetConvexHullIndices(VectorOfPoint contour)
        {
            VectorOfInt hullIndices = new VectorOfInt();
            CvInvoke.ConvexHull(contour, hullIndices, false, false);
            return hullIndices;
        }

        public Mat GetConvexityDefects(VectorOfPoint contour, VectorOfInt hullIndices)
        {
            var defectsMat = new Mat();
            CvInvoke.ConvexityDefects(contour, hullIndices, defectsMat);
            return defectsMat;
        }

        public void DrawDefects(Mat image, VectorOfPoint contour, Mat defectsMat)
        {
            if (defectsMat.IsEmpty || defectsMat.Rows == 0)
            {
                Debug.WriteLine("No defects found.");
                return;
            }

            // Each defect is 4 ints (startIdx, endIdx, farIdx, depth), each int is 4 bytes = 16 bytes per defect
            int defectSize = 16;
            int totalBytes = defectsMat.Rows * defectSize;
            byte[] data = new byte[totalBytes];
            System.Runtime.InteropServices.Marshal.Copy(defectsMat.DataPointer, data, 0, totalBytes);

            Debug.WriteLine($"Defects count: {defectsMat.Rows}");

            for (int i = 0; i < defectsMat.Rows; i++)
            {
                int startIdx = BitConverter.ToInt32(data, i * 16 + 0);
                int endIdx = BitConverter.ToInt32(data, i * 16 + 4);
                int farIdx = BitConverter.ToInt32(data, i * 16 + 8);
                float depth = BitConverter.ToSingle(data, i * 16 + 12) / 256.0f;

                Debug.WriteLine($"Defect {i}: startIdx={startIdx}, endIdx={endIdx}, farIdx={farIdx}, depth={depth}");
                if (startIdx >= 0 && startIdx < contour.Size &&
                    endIdx >= 0 && endIdx < contour.Size &&
                    farIdx >= 0 && farIdx < contour.Size)
                {
                    var startPoint = contour[startIdx];
                    var endPoint = contour[endIdx];
                    var farPoint = contour[farIdx];

                    CvInvoke.Line(image, startPoint, farPoint, new MCvScalar(0, 255, 0), 2);
                    CvInvoke.Line(image, farPoint, endPoint, new MCvScalar(0, 255, 0), 2);
                    CvInvoke.Circle(image, farPoint, 5, new MCvScalar(0, 0, 255), -1);
                }
            }
        }

        public Mat DrawConvexityDefects(VectorOfPoint contour, Mat inputImage)
        {
            if (contour == null || contour.Size < 4 || inputImage == null || inputImage.IsEmpty)
                return inputImage?.Clone() ?? new Mat();

            var debugImage = inputImage.Clone();

            var hullIndices = GetConvexHullIndices(contour);

            var defectsMat = GetConvexityDefects(contour, hullIndices);

            DrawDefects(debugImage, contour, defectsMat);

            return debugImage;
        }


    }

}
