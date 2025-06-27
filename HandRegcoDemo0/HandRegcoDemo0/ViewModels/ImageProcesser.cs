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
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
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
        public bool IsValidHandContour(VectorOfPoint contour, int imageHeight)
        {
            double area = CvInvoke.ContourArea(contour);
            if (area < 4000 || area > 90000)
                return false;

            var rect = CvInvoke.BoundingRectangle(contour);
            double aspect = (double)rect.Width / rect.Height;
            if (aspect < 0.2 || aspect > 2.0)
                return false;

            Moments m = CvInvoke.Moments(contour);
            if (m.M00 == 0)
                return false;

            int cy = (int)(m.M01 / m.M00);
            if (cy > imageHeight * 0.85)
                return false;
            return true;
            
        }
        public VectorOfPoint FindLargestContour(Mat skinMask)
        {
            var contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(skinMask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            double maxArea = 0;
            VectorOfPoint largest = null;

            for (int i = 0; i < contours.Size; i++)
            {
                var contour = contours[i];

                if (!IsValidHandContour(contour, skinMask.Rows))
                    continue;

                double area = CvInvoke.ContourArea(contour);
                if (area > maxArea)
                {
                    maxArea = area;
                    largest = contour;
                }
            }

            return largest;
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

            if (!IsValidContour(contour)|| !IsValidForConvexityDefects(contour, out VectorOfInt hullIndices))
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

            if (contour == null || contour.Size < 2)
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

        public bool IsValidContour(VectorOfPoint contour)
        {
            if (contour == null)
            {
                Debug.WriteLine("Contour is null.");
                return false;
            }

            if (contour.Size < 3)
            {
                Debug.WriteLine($"Contour too small. Size: {contour.Size}");
                return false;
            }

            try
            {
                double arcLength = CvInvoke.ArcLength(contour, true);
                if (arcLength <= 0 || double.IsNaN(arcLength))
                {
                    Debug.WriteLine("Contour arc length is invalid.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error checking contour: " + ex.Message);
                return false;
            }

            return true;
        }

        public VectorOfPoint EnsureInt32Contour(VectorOfPoint contour)
        {
            if (!IsValidContour(contour))
            {
                Debug.WriteLine("EnsureInt32Contour: Contour is null or too small.");
                return null;
            }

            return contour;
        }




    }
    
}
