using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.Models
{
    public class Segment
    {
        public Point highesPoint { get; set; }
        public Point LowestPoint { get; set; }
        public Point MostLeftPoint { get; set; }
        public Point MostRightPoint { get; set; }
    }
}
