using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.ViewModels
{
    class DistanceCalculation
    {
        public double getDistance(System.Drawing.Point a, System.Drawing.Point b)
        {
            double result = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            return result;
        }


    }
}
