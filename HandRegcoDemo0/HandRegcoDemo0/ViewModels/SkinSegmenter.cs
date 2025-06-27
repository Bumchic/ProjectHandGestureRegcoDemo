using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;

namespace HandRegcoDemo0.Utils.Segmentation
{
    /// <summary>
    /// Trách nhiệm: xử lý tách da tay từ ảnh đầu vào bằng cách dùng không gian màu YCrCb.
    /// </summary>
    public class SkinSegmenter
    {
        /// <summary>
        /// Phát hiện vùng da bằng cách dùng không gian màu YCrCb và ngưỡng giá trị Cr, Cb.
        /// </summary>
        /// <param name="inputMat">Ảnh đầu vào RGB</param>
        /// <returns>Ảnh nhị phân mask vùng da</returns>
        public Mat DetectSkinVer1(Mat inputMat)
        {
            if (inputMat == null || inputMat.IsEmpty)
                throw new ArgumentNullException(nameof(inputMat), "Input Mat is null or empty.");

            Mat yCrcb = new Mat();
            CvInvoke.CvtColor(inputMat, yCrcb, ColorConversion.Bgr2YCrCb);

            Mat skinMask = new Mat();
            CvInvoke.InRange(
                yCrcb,
                new ScalarArray(new MCvScalar(0, 133, 77)),
                new ScalarArray(new MCvScalar(255, 173, 127)),
                skinMask);

            CvInvoke.GaussianBlur(skinMask, skinMask, new Size(5, 5), 0);

            return skinMask;
        }
    }
}
