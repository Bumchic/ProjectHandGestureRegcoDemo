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
using HandRegcoDemo0.Utils.Segmentation;
using Windows.Graphics.Imaging;
using Avalonia.Media.Imaging;

namespace HandRegcoDemo0.ViewModels
{
    partial class ImageProcesser
    {
        //Minh
        public WriteableBitmap ProcessMat(SoftwareBitmap softwareBitmap, out string word)
        {
            HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();
            ImageConverter imageConverter = new ImageConverter();
            SkinSegmenter skinSegmenter = new SkinSegmenter();
            Draw draw = new Draw();
            Mat originalMat = imageConverter.ConvertToMat(softwareBitmap);
            Mat skinMaskMat = DetectSkinFromImage(softwareBitmap);
            word = "?";

            var handContour = handShapeAnalyzer.FindLargestContour(skinMaskMat);
            if (handContour == null || handContour.Size < 3)
                return imageConverter.MatToWriteableBitmap(originalMat);

            handContour = handShapeAnalyzer.PolyLineApprox(handContour);
            if (handContour == null || handContour.Size < 3)
                return imageConverter.MatToWriteableBitmap(originalMat);

            var handConvex = handShapeAnalyzer.GetConvexHull(handContour);
            if (handConvex == null || handConvex.Size < 3)
                return imageConverter.MatToWriteableBitmap(originalMat);

            CvInvoke.DrawContours(originalMat, new VectorOfVectorOfPoint(handConvex), -1, new Emgu.CV.Structure.MCvScalar(0, 255, 0), 2);

            RotatedRect box = CvInvoke.MinAreaRect(handContour);
            originalMat = draw.MarkMinAreaRect(box, originalMat);

            var hullIndices = handShapeAnalyzer.GetConvexHullIndices(handConvex);
            if (hullIndices == null || hullIndices.Size < 3)
                return imageConverter.MatToWriteableBitmap(originalMat);

            var defectsMat = handShapeAnalyzer.GetConvexityDefects(handContour);
            if (defectsMat == null || defectsMat.Rows == 0)
                return imageConverter.MatToWriteableBitmap(originalMat); // Không có defect nào

            originalMat = draw.DrawConvexDefect(originalMat, defectsMat, handContour);
            var recognized = _signRecognizer.Recognize(skinMaskMat);
            word = recognized;
           originalMat = draw.DrawText(originalMat, recognized);
            return imageConverter.MatToWriteableBitmap(originalMat);
            //ProcessedBitmapImage = _imageProcessor.MatToWriteableBitmap(processedMat);

        }
        private Mat DetectSkinFromImage(SoftwareBitmap softwareBitmap)
        {
            HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();
            ImageConverter imageConverter = new ImageConverter();
            SkinSegmenter skinSegmenter = new SkinSegmenter();
            Draw draw = new Draw();
            var inputMat = imageConverter.ConvertToMat(softwareBitmap);
            var processedMat = imageConverter.ColorConvertToGray(inputMat);

            var skinMaskMat = skinSegmenter.DetectSkinVer1(inputMat);
            return skinMaskMat;
        }
        private Mat calculateDistanceTransformation(Mat image)
        {
            CvInvoke.DistanceTransform(image, image, null, DistType.User, 0);
            return image;
        }


        
    }

}
