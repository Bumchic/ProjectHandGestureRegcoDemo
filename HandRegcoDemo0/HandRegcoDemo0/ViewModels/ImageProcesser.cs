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
using Avalonia.Controls.Templates;
using Windows.Devices.PointOfService;
using HandRegcoDemo0.Models;
using HandRegcoDemo0.NullClass;


namespace HandRegcoDemo0.ViewModels
{
    public partial class ImageProcesser
    {
        private readonly Rgba red = new Rgba(0, 0, 255, 255);
        private readonly Rgba green = new Rgba(0, 255, 0, 255);

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
            VectorOfPoint largestContour = new NullVectorOfPoint();

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
        
        public VectorOfInt GetConvexHullIndices(VectorOfPoint contour)
        {
            VectorOfInt hullIndices = new VectorOfInt();
            CvInvoke.ConvexHull(contour, hullIndices, false, false);
            return hullIndices;
        }

        public Mat GetConvexityDefects(VectorOfPoint contour)
        {
            var defectsMat = new Mat();

            if (!IsValidForConvexityDefects(contour, out VectorOfInt hullIndices))
            {
                return defectsMat;
            }

            try
            {
                CvInvoke.ConvexityDefects(contour, hullIndices, defectsMat);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConvexityDefects failed: {ex.Message}");
            }

            return defectsMat;
        }


        private bool IsValidForConvexityDefects(VectorOfPoint contour, out VectorOfInt hullIndices)
        {
            hullIndices = new VectorOfInt();

            if (contour == null || contour.Size < 3)
            {
                Debug.WriteLine("Contour is null or too small.");
                return false;
            }

            CvInvoke.ConvexHull(contour, hullIndices, returnPoints: false, clockwise: false);

            if (hullIndices == null || hullIndices.Size < 3)
            {
                Debug.WriteLine("Hull indices too small or null.");
                return false;
            }

            return true;
        }


        /*public void DrawDefects(Mat image, VectorOfPoint contour)
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
        }*/

        //public Mat DrawConvexityDefects(VectorOfPoint contour, Mat inputImage)
        //{
        //    if (contour == null || contour.Size < 4 || inputImage == null || inputImage.IsEmpty)
        //        return inputImage?.Clone() ?? new Mat();

        //    var debugImage = inputImage.Clone();

        //    var hullIndices = GetConvexHullIndices(contour);

        //    var defectsMat = GetConvexityDefects(contour, hullIndices);

        //    DrawDefects(debugImage, contour, defectsMat);

        //    return debugImage;
        //}

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
            if(contour == null)
            {
                return new VectorOfPoint();
            }
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
        public Mat MarkMinAreaRect(Rectangle box, Mat image)
        {
            CvInvoke.Rectangle(image, box, red.MCvScalar);
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
                if(new DistanceArithmetic().getDistance(CheckPoint, hull[i]) > box.Size.Width / 10)
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

        public VectorOfPoint FindLargestContour(VectorOfVectorOfPoint contours)
        {
            if(contours == null)
            {
                return new VectorOfPoint();
            }
            double maxArea = 0;
            VectorOfPoint largestContour = new NullVectorOfPoint();

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

        public Mat DrawConvexDefect(Mat img, Mat convexDefect, VectorOfPoint contour)
        {
            Matrix<int> matrix = new Matrix<int>(convexDefect.Rows, convexDefect.Cols, convexDefect.NumberOfChannels);
            convexDefect.CopyTo(matrix);
            RotatedRect box = CvInvoke.MinAreaRect(contour);
            for (int i = 0; i < matrix.Rows; i++)
            {
                Point furthest = new Point(contour[matrix.Data[i, 2]].X, contour[matrix.Data[i, 2]].Y);
                CvInvoke.DrawMarker(img, furthest, green.MCvScalar, MarkerTypes.Cross, 40, 10);
            }
            return img;
        }
        

        public Mat calculateDistanceTransformation(Mat image)
        {
            CvInvoke.DistanceTransform(image, image, null, DistType.Welsch, 0);
            return image;
        }
        public VectorOfPoint PolyLineApprox(VectorOfPoint contour)
        {
            double Epsilon = 0.025*CvInvoke.ArcLength(contour, true);
            if (contour == null)
            {
                Debug.WriteLine("Contour is null.");
            }
            else if (contour.Size == 0)
            {
                Debug.WriteLine("Contour is empty.");
            }
            else
            {
                CvInvoke.ApproxPolyDP(contour, contour, Epsilon, true);
            }

            return contour;
        }
        public Mat DrawContour(VectorOfPoint contour, Mat inputMat)
        {
            if (inputMat == null)
            {
                inputMat = new Mat();
            }
            CvInvoke.DrawContours(inputMat, new VectorOfVectorOfPoint(contour), -1, red.MCvScalar, 2);
            return inputMat;
        }
        public Rectangle getBoundingBox(VectorOfPoint contour)
        {
            if(contour is null)
            {
                return new Rectangle();
            }
            return CvInvoke.BoundingRectangle(contour);
        }
        public Mat DrawSinglePoint(Point point, Mat img)
        {
            if (point.IsEmpty)
            {
                return img;
            }
            CvInvoke.DrawMarker(img, point, red.MCvScalar, MarkerTypes.Star, thickness: 10);
            return img;
        }
        public Mat DrawSegment(Segment[] segmentlist, Mat img)
        {
            foreach(Segment segment in segmentlist)
            {
               img = DrawSinglePoint(segment.higestPoint, img);
               img = DrawSinglePoint(segment.lowestPoint, img);
                img = DrawSinglePoint(segment.highestMiddlePoint, img);
                img = DrawSinglePoint(segment.rightMostPoint, img);
                img = DrawSinglePoint(segment.leftMostPoint, img);
            }
            return img;
        }
        public Mat DrawSegmentTest(Segment[] segmentlist, Mat img)
        {
            img = DrawSinglePoint(segmentlist[1].highestMiddlePoint, img);
            img = DrawSinglePoint(segmentlist[1].highestMiddlePoint, img);
            img = DrawSinglePoint(segmentlist[1].lowestPoint, img);
           
            return img;
        }
        public int getConvexDefectCount(Mat convexDefect)
        {
            Matrix<int> matrix = new Matrix<int>(convexDefect.Rows, convexDefect.Cols, convexDefect.NumberOfChannels);
            convexDefect.CopyTo(matrix);
            return matrix.Data.GetLength(0);
        }
        public int getConvexHullCount(VectorOfPoint hull)
        {
            return hull.Size;
        }
        public Mat getFittingLine(VectorOfPoint contour)
        {
            Mat line = new Mat();
            CvInvoke.FitLine(contour, line, DistType.L1, 0, 0.01, 0.01);
            return line;
        }
        public Mat drawFittingLine(Mat line, Mat img)
        {
            Matrix<int> matrix = new Matrix<int>(line.Rows, line.Cols, line.NumberOfChannels);
            line.CopyTo(matrix);

            for(int i=0; i<matrix.Data.GetLength(0); i++)
            {
                for(int j=0; j < matrix.Data.GetLength(1); j++)
                {
                    Debug.Write(matrix.Data[i, j] );
                }
                Debug.Write("\n");
            }
            return img;
        }

    }

}
