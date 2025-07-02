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

public class SignRecognizer
{
    private readonly List<float[]> huData = new();
    private List<Mat> DescriptorData = new List<Mat>();
    private BFMatcher DescipData = new BFMatcher(DistanceType.Hamming);
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


            Mat Descriptor = handShapeAnalyzer.findInterestPoints(colorImage);
            DescriptorData.Add(Descriptor);
            //var hu = ComputeHuMoments(contour);
            //huData.Add(hu.Select(x => (float)x).ToArray());

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

        // Convert to Mat
        //var trainData = new Matrix<float>(huData.Count, 7);
        //for (int i = 0; i < huData.Count; i++)
        //{
        //    for (int j = 0; j < 7; j++)
        //        trainData[i, j] = huData[i][j];
        //}
        // Matrix<float> trainer = new Matrix<float>(DescriptorData.Count, DescriptorData[0].ElementSize);
        //Mat trainer = new Mat();
        foreach (Mat descriptor in DescriptorData)
        {
            
            DescipData.Add(descriptor);
        }
        DescipData.Train();
        var responses = new Matrix<int>(labels.ToArray());
        try
        {
           // knn.Train(trainMatrix, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, responses);
        }catch(Exception e)
        {
            Debug.WriteLine(e.Message + "" + e.Source);
        }

        Console.WriteLine($"Trained k-NN with {huData.Count} samples.");
    }


    public string Recognize(Mat inputImage)
    {
        HandShapeAnalyzer handShapeAnalyzer = new HandShapeAnalyzer();

        //var inputHu = ComputeHuMoments(contour);
        //var inputMat = new Matrix<float>(1, 7);
        //for (int i = 0; i < 7; i++)
        //    inputMat[0, i] = (float)inputHu[i];
        Mat inputDes = handShapeAnalyzer.findInterestPoints(inputImage);
        //Mat input = inputMat.Reshape(0, 1);
        //input.ConvertTo(input, DepthType.Cv32F);
        //Matrix<float> inputMatrix = new Matrix<float>(input.Rows, input.Cols, input.NumberOfChannels);
        //input.CopyTo(inputMatrix);
        var results = new Matrix<float>(1, 1);
        var neighborResponses = new Matrix<float>(1, 3); 
        var dists = new Matrix<float>(1, 3);
        VectorOfDMatch match = new VectorOfDMatch();
        VectorOfVectorOfDMatch matchArray = new VectorOfVectorOfDMatch();
        try
        {
            DescipData.Match(inputDes, match);
            //knn.FindNearest(inputMatrix, k: 3, results, neighborResponses, dists);

        }catch(Exception e)
        {
            Debug.WriteLine(e.Message + "" + e.Source);

        }

        VectorOfDMatch matchOrder = new VectorOfDMatch(match.ToArray().OrderByDescending(a => a.Distance).ToArray());
        int predictedId = matchOrder[matchOrder.Size -1].ImgIdx;
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
