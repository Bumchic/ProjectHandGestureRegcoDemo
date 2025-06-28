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
using Windows.Graphics.Imaging;
using Emgu.CV.Structure;
using Emgu.CV.Reg;
using Emgu.CV.Util;
using System.Drawing;
using Point = System.Drawing.Point;
using Avalonia.Controls.Templates;
using System.Net.Mime;
using System.Data;
using DynamicData;
using System.Windows.Forms;


namespace HandRegcoDemo0.ViewModels
{
    public partial class ImageProcesser
    {
        private readonly Rgba red = new Rgba(0, 0, 255, 255);
        private readonly Rgba green = new Rgba(0, 255, 0, 255);
        public SignRecognizer _signRecognizer = new SignRecognizer();
        public ImageProcesser()
        {
            _signRecognizer.LoadDataset("Datasets");
        }     
        public VectorOfPoint ReduceContour(VectorOfPoint largestContour)
        {
            Rectangle box = getBoudingBox(largestContour);
            List<Point> outOfBox = largestContour.ToArray().Where(a => a.Y < box.Y + box.Height).ToList();
            VectorOfPoint reducedContour = new VectorOfPoint(outOfBox.ToArray());
            return reducedContour;
        }
        public Point[] getLargestHandWidth(VectorOfPoint contour)
        {
            Point[] line = new Point[2];
            Rectangle box = CvInvoke.BoundingRectangle(contour);
            double longestLine = 0;
            Point[] contourArray = contour.ToArray();
            for(int i=box.Y; i <= box.Y + box.Height; i++)
            {
                Point[] Line = contourArray.Where(a => a.Y == i).ToArray();
                int intersectCount = Line.Length;
                if(intersectCount != 2)
                {
                    continue;
                }
                double distance = getDistance(Line[0], Line[1]);
                if (distance > longestLine)
                {
                    longestLine = distance;
                    line = Line;
                }
            }
            return line;
        }
        public Mat DrawPoints(Point[] points, Mat img)
        {
            for(int i=1; i<points.Length; i++)
            {
                CvInvoke.Line(img, points[i], points[i - 1], blue.MCvScalar, 20);
            }
            return img;
        }
        public int FindDefectWithLargestStartEndLength(VectorOfPoint contour, Mat convexDefect)
        {
            Matrix<int> matrix = new Matrix<int>(convexDefect.Rows, convexDefect.Cols, convexDefect.NumberOfChannels);
            convexDefect.CopyTo(matrix);

            double longestLength = 0;
            int index = 0;

            for(int i=0; i<matrix.Data.GetLength(0); i++)
            {
                int startIndex = matrix.Data[i, 0];
                int endIndex = matrix.Data[i, 1];
                int defectIndex = matrix.Data[i, 2];
                double length = getDistance(contour[startIndex], contour[endIndex]);

                if (length > longestLength)
                {
                    longestLength = length;
                    index = i;
                }
            }
            return index;
        }
        public Rectangle getBoudingBox(VectorOfPoint contour)
        {
            Rectangle box =CvInvoke.BoundingRectangle(contour);
            Mat convexDefect = GetConvexityDefects(contour);
            Matrix<int> matrix = new Matrix<int>(convexDefect.Rows, convexDefect.Cols, convexDefect.NumberOfChannels);
            convexDefect.CopyTo(matrix);
            int convexIndex = FindDefectWithLargestStartEndLength(contour, convexDefect);
            if(box.Height > contour[matrix.Data[convexIndex, 2]].Y - box.Y)
            {

                    Debug.WriteLine("Lower");
                    box.Height = contour[matrix.Data[convexIndex, 2]].Y - box.Y;
 
            }
            return box;
        }
    }
}
