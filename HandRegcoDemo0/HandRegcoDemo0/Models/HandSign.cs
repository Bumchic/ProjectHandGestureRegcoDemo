using DynamicData;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using HandRegcoDemo0.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public HandSign(Mat img):this()
        {
            CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.Bgr2Bgra);
            this.img = img;
        }
        public HandSign()
        {
            this.Word = "Empty";
        }
        public HandSign(Mat img, string word)
        {
            CvInvoke.CvtColor(img, img, Emgu.CV.CvEnum.ColorConversion.Bgr2Bgra);
            this.img = img;
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
