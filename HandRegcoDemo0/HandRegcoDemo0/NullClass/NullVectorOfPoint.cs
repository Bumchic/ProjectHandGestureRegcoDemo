using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.NullClass
{
    class NullVectorOfPoint : VectorOfPoint
    {
        public NullVectorOfPoint() : base()
        {
            List<Point> points = new List<Point>();
            points.Add(new Point(0, 0));
            points.Add(new Point(1, 0));
            points.Add(new Point(1, 1));
            points.Add(new Point(0, 1));
            this.Push(points.ToArray());
        }

    }
}
