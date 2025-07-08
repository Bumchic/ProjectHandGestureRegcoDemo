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

            originalMat = draw.DrawContour(handContour, originalMat);
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
            var inputMat = imageConverter.ConvertToMat(softwareBitmap);
            var processedMat = imageConverter.ColorConvertToGray(inputMat);

            var skinMaskMat = skinSegmenter.DetectSkinVer1(inputMat);
            return skinMaskMat;
        }
        private Mat DetectSkinFromImage(Mat inputMat)
        {
            HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();
            ImageConverter imageConverter = new ImageConverter();
            SkinSegmenter skinSegmenter = new SkinSegmenter();
            var processedMat = imageConverter.ColorConvertToGray(inputMat);

            var skinMaskMat = skinSegmenter.DetectSkinVer1(inputMat);
            return skinMaskMat;
        }
        public WriteableBitmap FocusOnHand(SoftwareBitmap softwareBitmap, out string word)
        {
            HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();
            ImageConverter imageConverter = new ImageConverter();
            SkinSegmenter skinSegmenter = new SkinSegmenter();
            //Mat originalMat = imageConverter.ConvertToMat(softwareBitmap);
            Mat originalMat = test();
            Mat skinMat = DetectSkinFromImage(originalMat);
            
            //originalMat = removeBackground(originalMat, skinMat);
            VectorOfPoint handContour = handShapeAnalyzer.FindLargestContour(skinMat);
            VectorOfPoint convexhull = handShapeAnalyzer.GetConvexHull(handContour);
            skinMat = new Draw().DrawContour(handContour, skinMat);
            originalMat = new Draw().DrawContour(convexhull, originalMat);
            //originalMat = new Draw().DrawContour(handContour, originalMat);
            word = "?";
            if (handContour is null || handContour.Size < 3)
            {
                return imageConverter.MatToWriteableBitmap(originalMat);
            }
            Rectangle box = CvInvoke.BoundingRectangle(handContour);
            Mat handImage = new Mat(skinMat, box);
            handImage = new Draw().DrawKeyPoints(handShapeAnalyzer.DetectKeyPoint(handImage), handImage);
            word = _signRecognizer.Recognize(originalMat);
            return imageConverter.MatToWriteableBitmap(handImage);
        }
        public Mat test()
        {
            Mat a = CvInvoke.Imread("DataHand\\U.jpg", ImreadModes.Unchanged);
            CvInvoke.CvtColor(a, a, ColorConversion.Bgr2Bgra);
            return a;
        }
        private Mat calculateDistanceTransformation(Mat image)
        {
            CvInvoke.DistanceTransform(image, image, null, DistType.User, 0);
            return image;
        }
        public Mat removeBackground(Mat originalMat, Mat skinMat)
        {
            Mat[] splitChannel = originalMat.Split();
            foreach(Mat channel in splitChannel)
            {
                CvInvoke.BitwiseAnd(channel, skinMat, channel);
            }
            Mat filtered = new Mat();
            VectorOfMat filterVector = new VectorOfMat(splitChannel);
            CvInvoke.Merge(filterVector, filtered);
            return filtered;
        }


        
    }

}
