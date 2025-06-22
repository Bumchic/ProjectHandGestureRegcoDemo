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
        public Point higestPoint { get; set; }
        public Point lowestPoint { get; set; }
        public Point highestMiddlePoint { get; set; }
        public Point leftMostPoint { get; set; }
        public Point rightMostPoint { get; set; }
    }
}
