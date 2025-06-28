using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using HandRegcoDemo0.Utils;

namespace HandRegcoDemo0.ViewModels
{
    partial class Draw
    {
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
            ContourHelper _contourHelper = new ContourHelper();
            DistanceCalculation _distanceCalculation = new DistanceCalculation();
            if (!_contourHelper.IsValidContour(hull))
            {
                Debug.WriteLine("Contour is invalid.");
                return image;
            }
            List<System.Drawing.Point> fingerpoints = new List<System.Drawing.Point>();
            RotatedRect box = CvInvoke.MinAreaRect(hull);
            System.Drawing.Point CheckPoint = hull[0];
            for (int i = 1; i < hull.Size; i++)
            {
                if (_distanceCalculation.getDistance(CheckPoint, hull[i]) > box.Size.Width / 10)
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
        public Mat DrawConvexDefect(Mat img, Mat convexDefect, VectorOfPoint contour)
        {
            ContourHelper _contourHelper = new ContourHelper();

            if (img == null || convexDefect == null || convexDefect.IsEmpty || !_contourHelper.IsValidContour(contour))
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
        public Mat DrawContour(VectorOfPoint contour, Mat inputMat)
        {
            ContourHelper _contourHelper = new ContourHelper();
            if (inputMat == null)
            {
                inputMat = new Mat();
            }
            if (!_contourHelper.IsValidContour(contour))
            {
                Debug.WriteLine("contour is invalid.");
                return inputMat;
            }
            CvInvoke.DrawContours(inputMat, new VectorOfVectorOfPoint(contour), -1, red.MCvScalar, 2);
            return inputMat;
        }
        public Mat DrawText(Mat inputMat, string word)
        {
                    CvInvoke.PutText(
            inputMat, word,
            new System.Drawing.Point(10, 50),
            FontFace.HersheyComplex, 2.0,
            new Emgu.CV.Structure.MCvScalar(255, 0, 0), 3);
            return inputMat;
        }
        public Mat DrawLines(Point[] points, Mat img)
        {
            for (int i = 1; i < points.Length; i++)
            {
                CvInvoke.Line(img, points[i], points[i - 1], blue.MCvScalar, 20);
            }
            return img;
        }

    }
}
