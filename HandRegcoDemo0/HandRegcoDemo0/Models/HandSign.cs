using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using HandRegcoDemo0.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace HandRegcoDemo0.Models
{
    class HandSign
    {
        public string Word { get; set; }
        public VectorOfPoint contour { get; set; }
        public VectorOfPoint convexHull { get; set; }
        public Rectangle box { get; set; }
        public double distanceFromFirstCorner { get; set; }

        public HandSign() 
        {
            this.Word = "Empty";
            this.contour = new VectorOfPoint();
            this.convexHull = new VectorOfPoint();
        }

        public HandSign(Mat img, string word)
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
            this.Word = word;
        }

        public HandSign(Mat img) : this(img, "This is Input for Mat") { }

        public List<HandSign> PopulateHandSign()
        {
            DirectoryInfo directory = new DirectoryInfo("Assets");

            FileInfo[] infos = directory.GetFiles("*.jpg");
            List<HandSign> imagelist = new List<HandSign>();

            foreach (var file in infos)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(file.FullName);
                    Mat img = new Mat();
                    CvInvoke.Imdecode(bytes, Emgu.CV.CvEnum.ImreadModes.Unchanged, img);

                    string label = Path.GetFileNameWithoutExtension(file.Name);

                    HandSign handSign = new HandSign(img, label);
                    imagelist.Add(handSign);

                    Console.WriteLine($"Loaded {file.Name} → Label: {label}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load {file.Name}: {ex.Message}");
                }
            }

            return imagelist;
        }
    }
}
