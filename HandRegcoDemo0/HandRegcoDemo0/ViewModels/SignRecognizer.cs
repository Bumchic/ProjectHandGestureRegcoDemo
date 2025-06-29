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
using HandRegcoDemo0.Models;
using System.Drawing;

public class SignRecognizer
{
    private readonly List<float[]> huData = new();
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
            var contour = handShapeAnalyzer.FindLargestContour(skinMask);

            if (contour != null)
            {
                var hu = ComputeHuMoments(contour);
                huData.Add(hu.Select(x => (float)x).ToArray());

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
        }

        // Convert to Mat
        var trainData = new Matrix<float>(huData.Count, 7);
        for (int i = 0; i < huData.Count; i++)
        {
            for (int j = 0; j < 7; j++)
                trainData[i, j] = huData[i][j];
        }

        var responses = new Matrix<int>(labels.ToArray());

        knn.Train(trainData, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, responses);
        Console.WriteLine($"Trained k-NN with {huData.Count} samples.");
    }


    public string Recognize(Mat inputImage)
    {
        HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();
        var contour = handShapeAnalyzer.FindLargestContour(inputImage);
        if (contour == null) return "?";

        var inputHu = ComputeHuMoments(contour);
        var inputMat = new Matrix<float>(1, 7);
           for (int i = 0; i < 7; i++)
            inputMat[0, i] = (float)inputHu[i];

        var results = new Matrix<float>(1, 1);
        var neighborResponses = new Matrix<float>(1, 3); 
        var dists = new Matrix<float>(1, 3);

        knn.FindNearest(inputMat, k: 3, results, neighborResponses, dists);

        int predictedId = (int)results[0, 0];
        return labelMap.ContainsKey(predictedId) ? labelMap[predictedId] : "?";
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

    public List<Segment> CreateSegments(VectorOfPoint contour)
    {
        Rectangle box = CvInvoke.BoundingRectangle(contour);
        double segmentWidth = box.Width / 5;
        List<Segment> segments = new List<Segment>();
        List<Point>[] segmentFull = new List<Point>[5];
        for(int i=0; i<segmentFull.Length; i++)
        {
            segmentFull[i] = new List<Point>();
        }
        for(int i=0; i<contour.Length; i++)
        {
            int position = (int)Math.Floor((contour[i].X - box.X) / segmentWidth);
            segmentFull[position].Add(contour[i]);
        }

        foreach(List<Point> points in segmentFull)
        {
            Segment segment = new Segment();
            foreach(Point point in points)
            {
                if(point.Y > segment.highesPoint.Y)
                {
                    segment.highesPoint = point;
                }
                if(point.Y < segment.LowestPoint.Y)
                {
                    segment.LowestPoint = point;
                }
                if(point.X < segment.MostLeftPoint.X)
                {
                    segment.MostLeftPoint = point;
                }
                if(point.X > segment.MostRightPoint.X)
                {
                    segment.MostRightPoint = point;
                }
            }
        }
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
