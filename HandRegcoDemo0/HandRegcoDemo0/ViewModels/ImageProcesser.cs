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


namespace HandRegcoDemo0.ViewModels
{
    public partial class ImageProcesser
    {
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

        public Mat ProcessGesture(Mat inputMat)
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

        //public VectorOfPoint FindLargestContour(Mat skinMask)
        //{
        //    var contours = new VectorOfVectorOfPoint();
        //    CvInvoke.FindContours(skinMask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

        //    double maxArea = 0;
        //    VectorOfPoint largestContour = null;

        //    for (int i = 0; i < contours.Size; i++)
        //    {
        //        double area = CvInvoke.ContourArea(contours[i]);
        //        if (area > maxArea)
        //        {
        //            maxArea = area;
        //            largestContour = contours[i];
        //        }
        //    }


        //    return largestContour;
        //}


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
        //public Mat DetectSkinV2(Mat Image)
        //{
        //    Mat OutputImage = new Mat();
        //    Mat InRangeImage = new Mat();
        //    //CvInvoke.CvtColor(Image, OutputImage, ColorConversion.Bgra2Bgr);
        //    //CvInvoke.CvtColor(OutputImage, OutputImage, ColorConversion.Bgr2Hsv);

            
        //    OutputImage = CreateConvexHull(Image);
        //    //CvInvoke.CvtColor(OutputImage, OutputImage, ColorConversion.Hsv2Bgr);
        //    //CvInvoke.CvtColor(OutputImage, OutputImage, ColorConversion.Bgr2Bgra);
        //    //CvInvoke.CvtColor(OutputImage, OutputImage, ColorConversion.Gray2Bgra);
        //    return OutputImage;
        //}
        //public Mat CreateConvexHull(Mat image)
        //{
        //    Image<Gray, byte> grayImage = new Image<Gray, byte>(image.Size);
        //    CvInvoke.CvtColor(image, grayImage, ColorConversion.Bgra2Gray);
        //    //CvInvoke.GaussianBlur(image, image, new System.Drawing.Size(9,9), 1.76);
        //    int HueLower = 3;
        //    int HueUpper = 33;
        //    MCvScalar Lower = new Emgu.CV.Structure.MCvScalar(HueLower, 50, 50);
        //    MCvScalar Upper = new MCvScalar(HueUpper, 255, 255);
        //    ScalarArray ScalerLower = new ScalarArray(Lower);
        //    ScalarArray ScalerUpper = new ScalarArray(Upper);

        //    Image<Gray, byte> EmguIMG = new Image<Gray, byte>(image.Size);
        //    CvInvoke.Canny(grayImage, EmguIMG, 0, 255);
        //    //CvInvoke.InRange(image, ScalerLower, ScalerUpper, EmguIMG);
            
        //    Rgb red = new Rgb(255, 0, 0);
        //        VectorOfVectorOfPoint Contours = new VectorOfVectorOfPoint();
        //        Mat ImageTopo = new Mat();
        //    try
        //    {
        //        CvInvoke.FindContours(EmguIMG, Contours, ImageTopo, RetrType.External, ChainApproxMethod.ChainApproxSimple);
        //        Contours = FindLargestContour(Contours);
        //        CvInvoke.DrawContours(image, Contours, -1, red.MCvScalar,2);
                
        //    }
        //    catch(Exception e)
        //    {
        //        Debug.WriteLine(e.Message);
        //    }
        //    return image;
        //}
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
    }
    //
}
