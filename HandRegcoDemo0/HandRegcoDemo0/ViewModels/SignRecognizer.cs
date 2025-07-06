using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV;
using HandRegcoDemo0.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Emgu.CV.ML;
using HandRegcoDemo0.Utils.Segmentation;
using Emgu.CV.Structure;
using Windows.Storage.Pickers;
using Emgu.CV.Features2D;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV.Flann;
using System.Windows.Media;
using FluentAvalonia.Core;

public class SignRecognizer
{
    private readonly List<float[]> huData = new();
    private readonly List<Mat> listSkinMat = new List<Mat>();
    private readonly List<int> labels = new();
    private readonly Dictionary<int, string> labelMap = new();
    private KNearest knn;
    public SignRecognizer()
    {
        knn = new KNearest();
    }
    public void LoadDataset(string folderPath)
    {
        SkinSegmenter skinSegmenter = new SkinSegmenter();
        HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();
        huData.Clear();
        labels.Clear();
        labelMap.Clear();

        var imagePaths = Directory.GetFiles(folderPath, "*.jpg");
        int labelId = 0;

        foreach (var path in imagePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var label = fileName.Split(' ')[0];

            var colorImage = CvInvoke.Imread(path, ImreadModes.Color);
            var skinMask = skinSegmenter.DetectSkinVer1(colorImage);
            listSkinMat.Add(skinMask);
            //colorImage = new Draw().DrawContour(convex, colorImage);
            
            

            if (!labelMap.ContainsValue(label))
            {
                labelMap[labelId] = label;
                labels.Add(labelId);
                labelId++;
            }
            else
            {
                int existingId = labelMap.First(kvp => kvp.Value == label).Key;
                labels.Add(existingId);
            }
            
        }
        Console.WriteLine($"Trained k-NN with {huData.Count} samples.");
    }


    public string Recognize(Mat inputImage)
    {
        HandShapeAnalyzer analyzer = new HandShapeAnalyzer();
        VectorOfVectorOfDMatch matchArray = new VectorOfVectorOfDMatch();
        SkinSegmenter skinSegmenter = new SkinSegmenter();
        var skinMask = skinSegmenter.DetectSkinVer1(inputImage);
        VectorOfPoint contour = analyzer.FindLargestContour(skinMask);
        Rectangle inputbox = CvInvoke.BoundingRectangle(contour);
        Mat inputHandImage = new Mat(skinMask, inputbox);
        int lowestCount = 99999;
        int index = -1;
        for(int i=0; i<listSkinMat.Count; i++)
        {
            VectorOfPoint cprContour = analyzer.FindLargestContour(listSkinMat[i]);
            Rectangle box = CvInvoke.BoundingRectangle(cprContour);
            double ratioHeight = (double)inputbox.Height / box.Height;
            double ratioWidth = (double)inputbox.Width / box.Width;
            box.X += (int)Math.Round((inputbox.Height - box.Height) * ratioHeight);
            box.Y = (int)Math.Round((inputbox.Width - box.Width) * ratioWidth);
            box.Height += (int)Math.Round((inputbox.Height - box.Height) * ratioHeight);
            box.Width += (int)Math.Round((inputbox.Width - box.Width) * ratioWidth);
            Mat handImage = new Mat(listSkinMat[i], box);
            CvInvoke.Resize(handImage, handImage, inputHandImage.Size);
            int beforeSubtractCount = CvInvoke.CountNonZero(handImage);
            Mat output = new Mat();
            CvInvoke.Subtract(listSkinMat[i], inputHandImage, output);
            int pixelCount = CvInvoke.CountNonZero(output);
            if(pixelCount < lowestCount)
            {
                int pixellost = beforeSubtractCount - pixelCount;
                lowestCount = pixellost;
                index = i;
            }
        }
        int predictedId = index;
        return labelMap.ContainsKey(predictedId) ? labelMap[predictedId] : "?";
    }

    public int getBestImageMatch(VectorOfDMatch matches)
    {
        List<int> imgIndexList = new List<int>(); 
        List<(int, double)> descriptorDistance = new List<(int, double)>();
        double lowest = 999999;
        int outIndex = 0;
        for (int i = 0; i < matches.Size; i++)
        {
            int imgIndex = matches[i].ImgIdx;
            if (!imgIndexList.Contains(imgIndex))
            {
                imgIndexList.Add(imgIndex);
                descriptorDistance.Add((1, matches[i].Distance));
            }
            else
            {
                int index = imgIndexList.IndexOf(imgIndex);
                descriptorDistance[index] = (descriptorDistance[index].Item1 + 1, descriptorDistance[index].Item2 + matches[i].Distance);
            }
        }
        for(int i=0; i< descriptorDistance.Count; i++)
        {
            double avgDistance = descriptorDistance[i].Item2 / descriptorDistance[i].Item1;
            if (avgDistance < lowest)
            {
                lowest = avgDistance;
                outIndex = imgIndexList[i];
            }
        }
        return outIndex;

    }
    public void print(List<int> foundindex, List<double> indexDistance)
    {
        for(int i=0; i<foundindex.Count; i++)
        {
            Debug.WriteLine($"{foundindex[i]} {indexDistance[i]}");
        }
    }


    private double[] ComputeHuMoments(VectorOfPoint contour)
    {
        var moments = CvInvoke.Moments(contour);
        var huMat = new Emgu.CV.Mat();
        CvInvoke.HuMoments(moments, huMat);

        double[] hu = new double[7];
        huMat.CopyTo(hu);

        for (int i = 0; i < hu.Length; i++)
        {
            hu[i] = -1 * Math.Sign(hu[i]) * Math.Log10(Math.Abs(hu[i]) + 1e-10);
        }
        return hu;
    }


    private string FindBestMatch(double[] inputHu)
    {
        int bestIndex = -1;
        double minDistance = double.MaxValue;

        for (int i = 0; i < huData.Count; i++)
        {
            float[] huTrain = huData[i];

            double dist = 0;
            for (int j = 0; j < huTrain.Length; j++)
            {
                dist += Math.Abs(inputHu[j] - huTrain[j]);
            }

            if (dist < minDistance)
            {
                minDistance = dist;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            int labelId = labels[bestIndex];
            return labelMap[labelId];
        }

        return "Not found";
    }

}
