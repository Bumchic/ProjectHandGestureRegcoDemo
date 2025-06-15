using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.Models
{
    class HandSign
    {
        public string Word { get; set; }
        public VectorOfPoint contour { get; set; }
        public VectorOfPoint convexHull { get; set; }
    }
}
