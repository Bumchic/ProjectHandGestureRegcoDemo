using Emgu.CV;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.Models
{
    class HandSign
    {
        public string Word { get; set; }
        public Mat img { get; set; }
        public VectorOfPoint contour { get; set; }
        public Mat contourMat { get; set; }
        public HandSign()
        {
            Word = "?";
            img = new Mat();
            contour = new VectorOfPoint();
            contourMat = new Mat();
        }
    }
}
