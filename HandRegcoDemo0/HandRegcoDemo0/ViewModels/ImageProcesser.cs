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
    }
    
}
