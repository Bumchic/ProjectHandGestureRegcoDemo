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
        public Point ContactHighRight { get; set; }
        public Point ContactHighLeft { get; set; }
        public Point ContactLowRight { get; set; }
        public Point ContactLowLeft { get; set; }
    public Segment()
        {
            ContactHighRight = new Point(0, 999);
            ContactHighLeft = new Point(0, 999);
            ContactLowRight = new Point(0, 0);
            ContactLowLeft = new Point(0, 0);
        }
    }

}
