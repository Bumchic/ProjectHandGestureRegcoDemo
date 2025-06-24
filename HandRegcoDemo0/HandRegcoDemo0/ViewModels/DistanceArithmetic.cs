using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using HandRegcoDemo0.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HandRegcoDemo0.ViewModels
{
    class DistanceArithmetic
    {
        public double DistanceFromBoxFirstCorner(VectorOfPoint contour, Rectangle box)
        {
            PointF firstCornerF = new PointF(box.X, box.Y);
            PointF averagePointF = getAvgPoint(contour);
            return getDistance(firstCornerF, averagePointF);
        }
        public PointF PointFromBoxFirstCornerPoint(VectorOfPoint contour, Rectangle box)
        {
            PointF firstCornerF = new PointF(box.X, box.Y);
            PointF averagePointF = getAvgPoint(contour);
            return new PointF(averagePointF.X - firstCornerF.X, averagePointF.Y - firstCornerF.Y);
        }
        public PointF PointFromCornerConvexDefect(Mat convexDefect, VectorOfPoint contour, Rectangle box)
        {
            
            PointF firstCornerF = new PointF(box.X, box.Y);
            Matrix<int> matrix = new Matrix<int>(convexDefect.Rows, convexDefect.Cols, convexDefect.NumberOfChannels);
            convexDefect.CopyTo(matrix);
            if(matrix.Data.Length == 0 || contour.Size == 0)
            {
                return new Point(0, 0);
            }
            List<Point> points = new List<Point>();
            for (int i=0; i<matrix.Data.GetLength(0); i++)
            {
                points.Add(new Point(contour[matrix.Data[i, 2]].X, contour[matrix.Data[i, 2]].Y));
            }
            VectorOfPoint convexPoints = new VectorOfPoint(points.ToArray());
            PointF avgPointF = getAvgPoint(convexPoints);
        
            return new PointF(avgPointF.X - firstCornerF.X, avgPointF.Y = firstCornerF.Y);
        }
        private PointF getAvgPoint(VectorOfPoint contour)
        {
            if(contour.Size == 0)
            {
                return new Point(0, 0);
            }
            PointF averagePointF = new PointF(XAxisSum(contour)/ contour.Size, YAxisSum(contour)/ contour.Size);
            return averagePointF;
        }
        private int XAxisSum(VectorOfPoint contour)
        {
            if(contour is null)
            {
                return 0;
            }
            int sum = 0;

            for (int i = 0; i < contour.Size; i++)
            {
                sum += contour[i].X;
            }
            

            return sum;
        }
        private int YAxisSum(VectorOfPoint contour)
        {
            if (contour is null)
            {
                return 0;
            }
            int sum = 0;
            for(int i=0; i<contour.Size; i++)
            {
                sum += contour[i].Y;
            }
            return sum;
        }
        public double getDistance(System.Drawing.Point a, System.Drawing.Point b)
        {
            double result = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            return result;
        }
        public double getDistance(PointF a, PointF b)
        {
            double result = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            return result;
        }
        public double CalculateHullToBoxRatio(VectorOfPoint hull, Rectangle box)
        {
                return CvInvoke.ContourArea(hull) / (box.Size.Width * box.Size.Height);
        
        }
        public Segment[] getSegmentFromHull(VectorOfPoint contour, Rectangle box)
        {
            int productX = box.Width / 20;
            int BottomLim = box.Height/2;

                List<Point>[] segmentFullContourX = new List<Point>[30];
            for (int i=0; i<segmentFullContourX.Length; i++)
            {
                segmentFullContourX[i] = new List<Point>();
            }
            List<Segment> segmentlist = new List<Segment>();
            if(box.IsEmpty|| contour.Size == 0)
            {
                return segmentlist.ToArray();
            }
            for (int i = 0; i < contour.Size; i++)
            {
                segmentFullContourX[(contour[i].X - box.X)/productX].Add(new Point(contour[i].X, contour[i].Y));
            }
            for (int i=0; i<segmentFullContourX.Length; i++)
            {
                Segment segment = new Segment();
                int highestPoint = -9999;
                int lowestPoint = 9999;
                int rightPoint = 0;
                int leftPoint = 9999;
                foreach (Point point in segmentFullContourX[i])
                {
                    if(point.Y > highestPoint && point.Y <= BottomLim + box.Y)
                    {
                        segment.higestPoint = new Point(point.X -box.X, point.Y - box.Y);
                        highestPoint = point.Y;
                    }
                    if (point.Y < lowestPoint && point.Y <= BottomLim + box.Y)
                    {
                        segment.lowestPoint = new Point(point.X - box.X, point.Y - box.Y);
                        lowestPoint = point.Y;
                    }
                    //if(point.Y == highestPoint - lowestPoint/2)
                    //{
                    //    segment.highestMiddlePoint = new Point(point.X - box.X, point.Y - box.Y);
                    //}
                    if (point.X > rightPoint && point.Y <= BottomLim)
                    {
                        rightPoint = point.X;
                        segment.rightMostPoint = new Point(point.X - box.X, point.Y - box.Y);
                    }
                    if (point.X < leftPoint && point.Y <= BottomLim)
                    {
                        leftPoint = point.X;
                        segment.leftMostPoint = new Point(point.X - box.X, point.Y - box.Y);
                    }
                }
                segmentlist.Add(segment);
            }
            return segmentlist.ToArray();
        }
    }
}
