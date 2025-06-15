using DynamicData;
using Emgu.CV;
using Emgu.CV.Util;
using HandRegcoDemo0.ViewModels;
using System;
using System.Collections.Generic;
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
        public List<HandSign> PopulateHandSign()
        {
            ImageProcesser _imageProcessor = new ImageProcesser();
            DirectoryInfo directory = new DirectoryInfo("Assets");
            FileInfo[] infos = directory.GetFiles();
            List<HandSign> imagelist = new List<HandSign>();
            for (int i = 0; i < infos.Length; i++)
            {

                if (infos[i].Extension == ".jpg")
                {

                    HandSign handSign = new HandSign();
                    byte[] bytes = File.ReadAllBytes(infos[i].FullName);
                    Mat img = new Mat();
                    CvInvoke.Imdecode(bytes, Emgu.CV.CvEnum.ImreadModes.Unchanged, img);
                    CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.Bgr2Bgra);
                    img = _imageProcessor.DetectSkinVer1(img);
                    VectorOfPoint contour = _imageProcessor.FindLargestContour(img);
                    VectorOfPoint hull = _imageProcessor.GetConvexHull(contour);
                    handSign.contour = contour;
                    handSign.convexHull = hull;
                    handSign.Word = infos[i].Name.Substring(infos[i].Name.Length - 1);
                    imagelist.Add(handSign);
                }
            }
            return imagelist;
        }
        }
    }
