using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yolov5Net.Scorer;

namespace PassportInfoExtraction
{
    public class DetectionScoreComparer : IComparer<YoloPrediction>
    {
        public int Compare(YoloPrediction x, YoloPrediction y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            int delta_y = (int)((x.Rectangle.Y - y.Rectangle.Y) / 20);
            int delta_x = (int)(x.Rectangle.X - y.Rectangle.X);

            return delta_y * 10000 + delta_x;

        }
    }
}
