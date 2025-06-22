using DynamicData;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using HandRegcoDemo0.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.Models
{
    public class HandSign
    {
        public string Word { get; set; }
        public Mat img { get; set; }
        public VectorOfPoint contour { get; set; }
        public VectorOfPoint convexHull { get; set; }
        public Rectangle box { get; set; }
        public double distanceFromFirstCorner { get; set; }
        public PointF pointFromFirstCorner { get; set; }
        public int ConvexCount { get; set; }
        public int hullCount { get; set; }
        public Segment[] listOfSegment { get; set; }
        public HandSign(Mat image)
        {
            ImageProcesser _imageProcessor = new ImageProcesser();
            DistanceArithmetic distanceArithmetic = new DistanceArithmetic();
            CvInvoke.CvtColor(image, image, Emgu.CV.CvEnum.ColorConversion.Bgr2Bgra);
            Mat img = new Mat();
            image.CopyTo(img);
            image = _imageProcessor.DetectSkinVer1(image);
            VectorOfPoint contour = _imageProcessor.FindLargestContour(image);
            VectorOfPoint hull = _imageProcessor.GetConvexHull(contour);
            Rectangle box = _imageProcessor.getBoundingBox(contour);
            double distFromCorner = distanceArithmetic.DistanceFromBoxFirstCorner(contour, box);
            Mat convexDefect = _imageProcessor.GetConvexityDefects(contour);
            //PointF pointFromCorner = distanceArithmetic.PointFromCornerConvexDefect(convexDefect, contour, box);
            int convexCount = _imageProcessor.getConvexDefectCount(convexDefect);
            int hullCount = _imageProcessor.getConvexHullCount(contour);
            Segment[] segementlist = distanceArithmetic.getSegmentFromHull(contour, box);
            img = _imageProcessor.DrawSegment(segementlist, img);
            this.contour = contour;
            img = _imageProcessor.DrawContour(contour, img);
            this.convexHull = hull;
            this.box = box;
            this.distanceFromFirstCorner = distFromCorner;
            //this.pointFromFirstCorner = pointFromCorner;
            this.ConvexCount = convexCount;
            this.hullCount = hullCount;
            this.listOfSegment = segementlist;
            this.img = img;
            this.Word = "empty";
        }
        public HandSign()
        {
            this.Word = "Empty";
            this.contour = new VectorOfPoint();
            this.convexHull = new VectorOfPoint();
        }
        public HandSign(Mat img, string word): this(img)
        {
            this.Word = word;
        }
        public List<HandSign> PopulateHandSign()
        {
            DirectoryInfo directory = new DirectoryInfo("Assets");
            FileInfo[] infos = directory.GetFiles();
            List<HandSign> imagelist = new List<HandSign>();
            for (int i = 0; i < infos.Length; i++)
            {

                if (infos[i].Extension == ".jpg")
                {   
                    byte[] bytes = File.ReadAllBytes(infos[i].FullName);
                    Mat img = new Mat();
                    CvInvoke.Imdecode(bytes, Emgu.CV.CvEnum.ImreadModes.Unchanged, img);
                    HandSign handSign = new HandSign(img, infos[i].Name);
                    imagelist.Add(handSign);
                }
            }
            return imagelist;
        }
    }
}
