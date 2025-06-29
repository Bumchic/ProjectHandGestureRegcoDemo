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
        public Point highestPoint { get; set; }
        public Point LowestPoint { get; set; }
        public Point MostLeftPoint { get; set; }
        public Point MostRightPoint { get; set; }
    public Segment()
        {
            highestPoint = new Point(0, 999);
            LowestPoint = new Point(0, 0);
            MostLeftPoint = new Point(999, 0);
            MostRightPoint = new Point(0, 0);
        }
    }

}
