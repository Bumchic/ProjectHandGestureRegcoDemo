using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.ViewModels
{
    class DistanceArithmetic
    {
        public double DistanceFromBoxFirstCorner(VectorOfPoint contour, Rectangle box)
        {
            PointF firstCornerF = new PointF(box.X, box.Y);
            PointF averagePointF = new PointF(XAxisSum(contour)/contour.Length, YAxisSum(contour)/contour.Length);
            return getDistance(firstCornerF, averagePointF);
        }
        public PointF DistanceFromBoxFirstCornerPoint(VectorOfPoint contour, Rectangle box)
        {
            PointF firstCornerF = new PointF(box.X, box.Y);
            PointF averagePointF = new PointF(XAxisSum(contour) / contour.Length, YAxisSum(contour) / contour.Length);
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
    }
}
