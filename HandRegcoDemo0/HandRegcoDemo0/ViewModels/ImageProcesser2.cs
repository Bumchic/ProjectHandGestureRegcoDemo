using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.ViewModels
{
    partial class ImageProcesser
    {
        //Minh
        public VectorOfPoint GetConvexHull(VectorOfPoint contour)
        {
            if (!IsValidContour(contour))
            {
                Debug.WriteLine("Contour is invalid.");
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
            for (int i = 0; i < hull.Size; i++)
            {
                CvInvoke.DrawMarker(image, hull[i], red.MCvScalar, MarkerTypes.Cross, 30, 10);
            }
            return image;
        }
        public Mat MarkMinAreaRect(RotatedRect box, Mat image)
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            foreach (PointF pointf in box.GetVertices())
            {
                points.Add(System.Drawing.Point.Round(pointf));
            }
            VectorOfPoint vector = new VectorOfPoint(values: points.ToArray());
            CvInvoke.Polylines(image, vector, true, red.MCvScalar, 2);
            return image;
        }
        public Mat MarkFingerPoint(VectorOfPoint hull, Mat image)
        {
            if (!IsValidContour(hull))
            {
                Debug.WriteLine("Contour is invalid.");
                return image;
            }
            List<System.Drawing.Point> fingerpoints = new List<System.Drawing.Point>();
            RotatedRect box = CvInvoke.MinAreaRect(hull);
            System.Drawing.Point CheckPoint = hull[0];
            for (int i = 1; i < hull.Size; i++)
            {
                if (getDistance(CheckPoint, hull[i]) > box.Size.Width / 10)
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

        public Mat DrawConvexDefect(Mat img, Mat convexDefect, VectorOfPoint contour)
        {
            if (img == null || convexDefect == null || convexDefect.IsEmpty || !IsValidContour(contour))
            {
                Debug.WriteLine("Invalid input to DrawConvexDefect.");
                return img;
            }

            int defectSize = 4;
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
            contour = EnsureInt32Contour(contour);
            if (!IsValidContour(contour))
            {
                Debug.WriteLine("Contour is invalid.");
                return null;
            }

            double Epsilon = 0.025 * CvInvoke.ArcLength(contour, true);
            Debug.WriteLine(Epsilon);

            CvInvoke.ApproxPolyDP(contour, contour, Epsilon, true);
            return contour;
        }

        public Mat DrawContour(VectorOfPoint contour, Mat inputMat)
        {
            if (inputMat == null)
            {
                inputMat = new Mat();
            }
            if (!IsValidContour(contour))
            {
                Debug.WriteLine("contour is invalid.");
                return inputMat;
            }
            CvInvoke.DrawContours(inputMat, new VectorOfVectorOfPoint(contour), -1, red.MCvScalar, 2);
            return inputMat;
        }
    }

}
