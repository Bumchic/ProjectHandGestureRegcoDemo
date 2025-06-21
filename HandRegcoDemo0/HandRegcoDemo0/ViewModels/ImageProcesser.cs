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
                return null;
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

        public Mat DrawConvexDefect(Mat img, Mat convexDefect, VectorOfPoint contour)
        {
            if (convexDefect == null || convexDefect.IsEmpty || contour == null)
                return img;

            int defectSize = 4; // mỗi defect gồm 4 giá trị: startIdx, endIdx, farIdx, depth
            for (int i = 0; i < convexDefect.Rows; i++)
            {
                int[] defect = new int[defectSize];
                System.Runtime.InteropServices.Marshal.Copy(convexDefect.DataPointer + i * defectSize * sizeof(int), defect, 0, defectSize);

                int farIdx = defect[2];
                if (farIdx >= 0 && farIdx < contour.Size)
                {
                    Point furthest = contour[farIdx];
                    CvInvoke.DrawMarker(img, furthest, green.MCvScalar, MarkerTypes.Cross, 40, 10);
                }
            }

            return img;
        }

        public Mat calculateDistanceTransformation(Mat image)
        {
            CvInvoke.DistanceTransform(image, image, null, DistType.User, 0);
            return image;
        }
        public VectorOfPoint PolyLineApprox(VectorOfPoint contour)
        {
            double Epsilon = 0.025*CvInvoke.ArcLength(contour, true);
            Debug.WriteLine(Epsilon);
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
    }

}
