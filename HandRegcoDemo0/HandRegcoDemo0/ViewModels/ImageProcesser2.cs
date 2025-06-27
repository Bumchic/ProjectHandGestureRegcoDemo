using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.ViewModels
{
    partial class ImageProcesser
    {
        //Minh
       



        
        
        

        

        public Mat calculateDistanceTransformation(Mat image)
        {
            CvInvoke.DistanceTransform(image, image, null, DistType.User, 0);
            return image;
        }
       

        
    }

}
