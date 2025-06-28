using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using HandRegcoDemo0.ViewModels;
using HandRegcoDemo0.Utils;
using Emgu.CV.Structure;

namespace HandRegcoDemo0.ViewModels
{
    class HandShapeAnalyzer
    {
        private readonly Rgba red = new Rgba(0, 0, 255, 255);
        private readonly Rgba green = new Rgba(0, 255, 0, 255);
        public VectorOfPoint FindLargestContour(Mat skinMask)
        {
            ContourHelper contourHelper = new ContourHelper();
            var contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(skinMask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            double maxArea = 0;
            VectorOfPoint largest = null;

            for (int i = 0; i < contours.Size; i++)
            {
                var contour = contours[i];
                if (!contourHelper.IsValidHandContour(contour, skinMask.Rows))
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
        public VectorOfInt GetConvexHullIndices(VectorOfPoint contour)
        {
            VectorOfInt hullIndices = new VectorOfInt();
            CvInvoke.ConvexHull(contour, hullIndices, false, false);
            return hullIndices;
        }
        public Mat GetConvexityDefects(VectorOfPoint contour)
        {
            var defectsMat = new Mat();
            ContourHelper contourHelper = new ContourHelper();

            if (!contourHelper.IsValidContour(contour) || !contourHelper.IsValidForConvexityDefects(contour, out VectorOfInt hullIndices))
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
        public VectorOfPoint GetConvexHull(VectorOfPoint contour)
        {
            ContourHelper contourHelper = new ContourHelper();
            if (!contourHelper.IsValidContour(contour))
            {
                Debug.WriteLine("Contour is invalid.");
                return null;
            }
            VectorOfPoint hull = new VectorOfPoint(contour.Size);

            CvInvoke.ConvexHull(contour, hull, false, false);


            return hull;
        }
        public VectorOfPoint FindLargestContour(VectorOfVectorOfPoint contours)
        {
            if (contours == null)
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
        public VectorOfPoint PolyLineApprox(VectorOfPoint contour)
        {
            ContourHelper contourHelper = new ContourHelper();
            contour = contourHelper.EnsureInt32Contour(contour);
            if (!contourHelper.IsValidContour(contour))
            {
                Debug.WriteLine("Contour is invalid.");
                return null;
            }

            double Epsilon = 0.025 * CvInvoke.ArcLength(contour, true);
            Debug.WriteLine(Epsilon);

            CvInvoke.ApproxPolyDP(contour, contour, Epsilon, true);
            return contour;
        }
        
    }
}
