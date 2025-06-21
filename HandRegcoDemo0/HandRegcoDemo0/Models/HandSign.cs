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
    class HandSign
    {
        public string Word { get; set; }
        public VectorOfPoint contour { get; set; }
        public VectorOfPoint convexHull { get; set; }
        public Rectangle box { get; set; }
        public double distanceFromFirstCorner { get; set; }
        public PointF pointFromFirstCorner { get; set; }
        public HandSign(Mat img, string word)
        {
            ImageProcesser _imageProcessor = new ImageProcesser();
            CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.Bgr2Bgra);
            img = _imageProcessor.DetectSkinVer1(img);
            VectorOfPoint contour = _imageProcessor.FindLargestContour(img);
            VectorOfPoint hull = _imageProcessor.GetConvexHull(contour);
            Rectangle box = _imageProcessor.getBoundingBox(contour);
            double distFromCorner = new DistanceArithmetic().DistanceFromBoxFirstCorner(contour, box);
            PointF pointFromCorner = new DistanceArithmetic().DistanceFromBoxFirstCornerPoint(contour, box);
            this.contour = contour;
            this.convexHull = hull;
            this.box = box;
            this.distanceFromFirstCorner = distFromCorner;
            this.pointFromFirstCorner = pointFromCorner;
            this.Word = word;
        }
        public HandSign()
        {
            this.Word = "Empty";
            this.contour = new VectorOfPoint();
            this.convexHull = new VectorOfPoint();
        }
        public HandSign(Mat img)
        {
            ImageProcesser _imageProcessor = new ImageProcesser();
            CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.Bgr2Bgra);
            img = _imageProcessor.DetectSkinVer1(img);
            VectorOfPoint contour = _imageProcessor.FindLargestContour(img);
            VectorOfPoint hull = _imageProcessor.GetConvexHull(contour);
            Rectangle box = _imageProcessor.getBoundingBox(contour);
            double distFromCorner = new DistanceArithmetic().DistanceFromBoxFirstCorner(hull, box);
            this.contour = contour;
            this.convexHull = hull;
            this.box = box;
            this.distanceFromFirstCorner = distFromCorner;
            this.Word = "This is Input for Mat";
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
                    HandSign handSign = new HandSign(img, infos[i].Name.Substring(0, 1));
                    imagelist.Add(handSign);
                }
            }
            return imagelist;
        }
        
    }
}
