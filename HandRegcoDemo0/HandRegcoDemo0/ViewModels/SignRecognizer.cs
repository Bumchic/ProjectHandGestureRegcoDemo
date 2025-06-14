using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV;
using HandRegcoDemo0.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

public class SignRecognizer
{
    private readonly Dictionary<string, double[]> database = new();
    private readonly ImageProcesser _imageProcesser;
    public SignRecognizer()
    {
        _imageProcesser = new ImageProcesser();
    }
    public void LoadDataset(string folderPath)
    {
        var imagePaths = Directory.GetFiles(folderPath, "*.jpg");
        foreach (var path in imagePaths)
        {
            var label = Path.GetFileNameWithoutExtension(path).Replace("_test", "");
            var colorImage = CvInvoke.Imread(path, ImreadModes.Color);

            var skinMask = _imageProcesser.DetectSkinVer1(colorImage);
            var contour = _imageProcesser.FindLargestContour(skinMask); 

            if (contour != null)
            {
                var huMoments = ComputeHuMoments(contour);
                database[label] = huMoments;
            }
        }
        Console.WriteLine("Loaded sign: {string.Join(", ", database.Keys)}");
    }

    public string Recognize(Mat inputImage)
    {
        var contour = _imageProcesser.FindLargestContour(inputImage);
        if (contour == null) return "?";

        var inputHu = ComputeHuMoments(contour);
        return FindBestMatch(inputHu);
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
        string bestLabel = "?";
        double minDistance = double.MaxValue;

        foreach (var (label, huMoments) in database)
        {
            double dist = inputHu.Zip(huMoments, (a, b) => Math.Abs(Math.Log10(a) - Math.Log10(b))).Sum();
            if (dist < minDistance)
            {
                minDistance = dist;
                bestLabel = label;
            }
        }

        return bestLabel;
    }
}
