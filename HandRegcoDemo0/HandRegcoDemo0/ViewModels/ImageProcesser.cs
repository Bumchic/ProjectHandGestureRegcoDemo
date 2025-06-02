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
using Emgu.CV.Structure;
using Windows.Graphics.Imaging;
using Emgu.CV.Structure;
using Emgu.CV.Reg;
using Emgu.CV.Util;


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
            if (mat.NumberOfChannels != 4)
            {
                Mat converted = new Mat();
                CvInvoke.CvtColor(mat, converted, ColorConversion.Gray2Bgra);
                mat = converted;
            }
            byte[] data = new byte[mat.Rows * mat.Cols * mat.NumberOfChannels];
            mat.CopyTo(data);
            var bitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, mat.Cols, mat.Rows);
            bitmap.CopyFromBuffer(data.AsBuffer());
            return bitmap;
        }

        public WriteableBitmap MatToWriteableBitmap(Mat mat)
        {
            if (mat.NumberOfChannels != 4)
            {
                Mat converted = new Mat();
               CvInvoke.CvtColor(mat, converted, ColorConversion.Gray2Bgra);
               mat = converted;
            }

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

            for(int i = 0; i < countours.Size; i++)
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


    }
    partial class ImageProcesser
    {
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
        public WriteableBitmap DetectSkinV2(Mat Image)
        {
            int HueLower = 3;
            int HueUpper = 33;
            MCvScalar Lower = new Emgu.CV.Structure.MCvScalar(HueLower, 50, 50);
            MCvScalar Upper = new MCvScalar(HueUpper, 255, 255);
            ScalarArray ScalerLower = new ScalarArray(Lower);
            ScalarArray ScalerUpper = new ScalarArray(Upper);
            Mat OutputImage = new Mat();
            Mat ScalarOutput = new Mat();
            CvInvoke.CvtColor(Image, OutputImage, ColorConversion.Bgra2Bgr);
            CvInvoke.CvtColor(OutputImage, OutputImage, ColorConversion.Bgr2Hsv);
            CvInvoke.InRange(OutputImage, ScalerLower, ScalerUpper, ScalarOutput);
            CvInvoke.CvtColor(OutputImage, OutputImage, ColorConversion.Hsv2Bgr);
            CvInvoke.CvtColor(ScalarOutput, ScalarOutput, ColorConversion.Bgr2Bgra);

            WriteableBitmap bitmap = MatToWriteableBitmap(ScalarOutput);
            return bitmap;
        }
    }
    //
}
