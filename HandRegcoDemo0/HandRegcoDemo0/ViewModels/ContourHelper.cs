using System;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;

namespace HandRegcoDemo0.Utils
{
    /// <summary>
    /// Hỗ trợ xử lý các thao tác liên quan đến contour như kiểm tra, làm mượt, chọn contour lớn nhất.
    /// </summary>
    public class ContourHelper
    {
        /// <summary>
        /// Kiểm tra contour có hợp lệ không (null, quá nhỏ hoặc đường cong không hợp lệ).
        /// </summary>
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

        /// <summary>
        /// Đảm bảo contour có định dạng đúng
        /// </summary>
        public VectorOfPoint EnsureInt32Contour(VectorOfPoint contour)
        {
            if (!IsValidContour(contour))
            {
                Debug.WriteLine("EnsureInt32Contour: Contour is null or too small.");
                return null;
            }

            return contour;
        }

        /// <summary>
        /// Làm mượt contour bằng thuật toán ApproxPolyDP.
        /// </summary>
        public VectorOfPoint PolyLineApprox(VectorOfPoint contour)
        {
            contour = EnsureInt32Contour(contour);
            if (!IsValidContour(contour))
            {
                Debug.WriteLine("Contour is invalid.");
                return null;
            }

            double epsilon = 0.025 * CvInvoke.ArcLength(contour, true);
            CvInvoke.ApproxPolyDP(contour, contour, epsilon, true);
            return contour;
        }

        /// <summary>
        /// Tìm contour có diện tích lớn nhất từ danh sách contour.
        /// </summary>
        public VectorOfPoint FindLargestContour(VectorOfVectorOfPoint contours)
        {
            if (contours == null)
                return null;

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
        public bool IsValidHandContour(VectorOfPoint contour, int imageHeight)
        {
            double area = CvInvoke.ContourArea(contour);
            if (area < 8000 || area > 60000)
                return false;

            var rect = CvInvoke.BoundingRectangle(contour);
            double aspect = (double)rect.Width / rect.Height;
            if (aspect < 0.4 || aspect > 1.8)
                return false;

            Moments m = CvInvoke.Moments(contour);
            if (m.M00 == 0)
                return false;

            int cy = (int)(m.M01 / m.M00);
            if (cy > imageHeight * 0.75)
                return false;
            return true;

        }
        public bool IsValidForConvexityDefects(VectorOfPoint contour, out VectorOfInt hullIndices)
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
    }
}
