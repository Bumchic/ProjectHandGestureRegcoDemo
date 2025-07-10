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
using HandRegcoDemo0.Utils.Segmentation;


namespace HandRegcoDemo0.ViewModels
{
    public partial class ImageProcesser
    {
        private readonly Rgba red = new Rgba(0, 0, 255, 255);
        private readonly Rgba green = new Rgba(0, 255, 0, 255);
        private readonly SignRecognizer _signRecognizer; 
        public ImageProcesser()
        {
            _signRecognizer = new SignRecognizer();
            _signRecognizer.LoadDataset("Datahand - Copy");
        }
        public WriteableBitmap ProcessMat(SoftwareBitmap softwareBitmap, out string recognizedWord)
        {
            HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();
            ImageConverter imageConverter = new ImageConverter();
            SkinSegmenter skinSegmenter = new SkinSegmenter();
            Draw draw = new Draw();

            Mat inputMat = imageConverter.ConvertToMat(softwareBitmap);
            Mat skinMaskMat = skinSegmenter.DetectSkinVer1(inputMat);
            recognizedWord = "?";

            VectorOfPoint handContour = handShapeAnalyzer.FindLargestContour(skinMaskMat);
            if (handContour == null || handContour.Size < 3)
                return imageConverter.MatToWriteableBitmap(inputMat);

            handContour = handShapeAnalyzer.PolyLineApprox(handContour);
            if (handContour == null || handContour.Size < 3)
                return imageConverter.MatToWriteableBitmap(inputMat);

            var handConvex = handShapeAnalyzer.GetConvexHull(handContour);
            if (handConvex == null || handConvex.Size < 3)
                return imageConverter.MatToWriteableBitmap(inputMat);

            CvInvoke.DrawContours(inputMat, new VectorOfVectorOfPoint(handConvex), -1, new MCvScalar(0, 255, 0), 2);
            inputMat = draw.MarkMinAreaRect(CvInvoke.MinAreaRect(handContour), inputMat);

            var hullIndices = handShapeAnalyzer.GetConvexHullIndices(handConvex);
            if (hullIndices == null || hullIndices.Size < 3)
                return imageConverter.MatToWriteableBitmap(inputMat);

            var defectsMat = handShapeAnalyzer.GetConvexityDefects(handContour);
            if (defectsMat != null && defectsMat.Rows > 0)
            {
                inputMat = draw.DrawConvexDefect(inputMat, defectsMat, handContour);
            }

            recognizedWord = _signRecognizer.Recognize(skinMaskMat);
/*            inputMat = draw.Draw(inputMat, recognizedWord);
*/            return imageConverter.MatToWriteableBitmap(inputMat);
        }


    }

}
