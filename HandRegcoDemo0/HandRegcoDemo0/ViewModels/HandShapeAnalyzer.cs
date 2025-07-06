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
using System.Drawing;
using Emgu.CV.Features2D;

namespace HandRegcoDemo0.ViewModels
{
    class HandShapeAnalyzer
    {
        private readonly Rgba red = new Rgba(0, 0, 255, 255);
        private readonly Rgba green = new Rgba(0, 255, 0, 255);
        private readonly Rgba White = new Rgba(255, 255, 255, 255);
        private readonly int MaxFeature = 2000;
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
            if(largest is not null)
            {
                largest = ReduceContour(largest);
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
            largestContour = ReduceContour(largestContour);

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
        public VectorOfPoint ReduceContour(VectorOfPoint largestContour)
        {
            Rectangle box = getBoudingBoxAtWristLine(largestContour);
            List<Point> outOfBox = largestContour.ToArray().Where(a => a.Y < box.Y + box.Height).ToList();
            VectorOfPoint reducedContour = new VectorOfPoint(outOfBox.ToArray());
            return reducedContour;
        }
        public Point[] getLargestHandWidth(VectorOfPoint contour)
        {
            DistanceCalculation distanceCalculatior = new DistanceCalculation();
            Point[] line = new Point[2];
            Rectangle box = CvInvoke.BoundingRectangle(contour);
            double longestLine = 0;
            Point[] contourArray = contour.ToArray();
            for (int i = box.Y; i <= box.Y + box.Height; i++)
            {
                Point[] Line = contourArray.Where(a => a.Y == i).ToArray();
                int intersectCount = Line.Length;
                if (intersectCount != 2)
                {
                    continue;
                }
                double distance = distanceCalculatior.getDistance(Line[0], Line[1]);
                if (distance > longestLine)
                {
                    longestLine = distance;
                    line = Line;
                }
            }
            return line;
        }
        private Rectangle getBoudingBoxAtWristLine(VectorOfPoint contour)
        {
            HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();
            Rectangle box = CvInvoke.BoundingRectangle(contour);
            Mat convexDefect = handShapeAnalyzer.GetConvexityDefects(contour);
            Matrix<int> matrix = new Matrix<int>(convexDefect.Rows, convexDefect.Cols, convexDefect.NumberOfChannels);
            convexDefect.CopyTo(matrix);
            int convexIndex = FindDefectWithLargestStartEndLength(contour, convexDefect);
            if (box.Height > contour[matrix.Data[convexIndex, 2]].Y - box.Y)
            {

                Debug.WriteLine("Lower");
                box.Height = contour[matrix.Data[convexIndex, 2]].Y - box.Y;

            }
            return box;
        }
        private int FindDefectWithLargestStartEndLength(VectorOfPoint contour, Mat convexDefect)
        {
            Matrix<int> matrix = new Matrix<int>(convexDefect.Rows, convexDefect.Cols, convexDefect.NumberOfChannels);
            convexDefect.CopyTo(matrix);
            DistanceCalculation distanceCalculatior = new DistanceCalculation();
            double longestLength = 0;
            int index = 0;

            for (int i = 0; i < matrix.Data.GetLength(0); i++)
            {
                int startIndex = matrix.Data[i, 0];
                int endIndex = matrix.Data[i, 1];
                int defectIndex = matrix.Data[i, 2];
                double length = distanceCalculatior.getDistance(contour[startIndex], contour[endIndex]);

                if (length > longestLength)
                {
                    longestLength = length;
                    index = i;
                }
            }
            return index;
        }
        public Mat findInterestPoints(VectorOfPoint contour, Mat OriginalColorMat)
        {
            Mat handImage;
            Rectangle box;

            if (OriginalColorMat.NumberOfChannels != 1)
            {
                OriginalColorMat = new Utils.Segmentation.SkinSegmenter().DetectSkinVer1(OriginalColorMat);
            }
            ORB orb = new ORB(numberOfFeatures: MaxFeature, WTK_A: 4, edgeThreshold: 0);
            Mat Descriptor = new Mat();
            if(contour is not null)
            {
                box = CvInvoke.BoundingRectangle(contour);
                handImage = new Mat(OriginalColorMat, box);
            }
            else
            {
                handImage = OriginalColorMat;
            }
            MKeyPoint[] keypoints = orb.Detect(handImage);
            orb.Compute(handImage, new VectorOfKeyPoint(keypoints), Descriptor);
            return Descriptor;
        }
        public Mat findInterestPoints(Mat originalMat)
        {
            Mat skinMask = new HandRegcoDemo0.Utils.Segmentation.SkinSegmenter().DetectSkinVer1(originalMat);
            VectorOfPoint contour = FindLargestContour(skinMask);
            return this.findInterestPoints(contour, originalMat);
        }
        public MKeyPoint[] DetectKeyPoint(Mat img)
        {
            ORB orb = new ORB(numberOfFeatures: MaxFeature, WTK_A: 4, edgeThreshold: 0);
            return orb.Detect(img);
        }
        public Mat GetStandardHandImage(Mat input, VectorOfPoint contour)
        {
            int stdHeight = 300;
            int stdWidth = 250;
            if(contour is null)
            {
                return input;
            }
            Rectangle box = CvInvoke.BoundingRectangle(contour);
            box.X -= stdWidth - box.Width;
            box.Y -= stdHeight - box.Height;
            box.Width += stdWidth - box.Width;
            box.Height += stdHeight - box.Height;
            try
            {
                return new Mat(input, box);
            }catch(Exception e)
            {
                return input;
            }
            
        }
    }
}
