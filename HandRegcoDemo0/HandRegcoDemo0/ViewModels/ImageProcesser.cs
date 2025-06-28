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
        public SignRecognizer _signRecognizer = new SignRecognizer();
        public ImageProcesser()
        {
            _signRecognizer.LoadDataset("Datasets");
        }     
        
        

    }
}
