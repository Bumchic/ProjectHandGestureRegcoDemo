using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace HandRegcoDemo0.Utils
{
    public class ImageConverter
    {
        public Mat SoftwareBitmapToMat(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);

            byte[] data = new byte[4 * softwareBitmap.PixelWidth * softwareBitmap.PixelHeight];
            softwareBitmap.CopyToBuffer(data.AsBuffer());

            Mat mat = new Mat(softwareBitmap.PixelHeight, softwareBitmap.PixelWidth, DepthType.Cv8U, 4);
            mat.SetTo(data);
            return mat;
        }

        public SoftwareBitmap MatToSoftwareBitmap(Mat mat)
        {
            byte[] data = new byte[mat.Rows * mat.Cols * mat.NumberOfChannels];
            mat.CopyTo(data);
            var bitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, mat.Cols, mat.Rows);
            bitmap.CopyFromBuffer(data.AsBuffer());
            return bitmap;
        }

        public unsafe WriteableBitmap MatToWriteableBitmap(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int stride = width * 4;
            byte[] bytes = new byte[4 * height * stride];
            mat.CopyTo(bytes);

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

        public unsafe WriteableBitmap SoftwareBitmapToWriteableBitmap(SoftwareBitmap softwareBitmap)
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
                return new WriteableBitmap(pixelFormat, alphaFormat, intptr, pixelSize, dpi, stride);
            }
        }
    }
}
