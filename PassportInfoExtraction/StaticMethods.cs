using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yolov5Net.Scorer;

namespace PassportInfoExtraction
{
    public static class StaticMethods
    {
        public static float delta_x = 50;
        public static float delta_y = 10;
        public static int CompareYoloPrediction(YoloPrediction x, YoloPrediction y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            if (x.Rectangle.Y - y.Rectangle.Y > delta_y)
            {
                return 1;
            }

            if (x.Rectangle.Y - y.Rectangle.Y < -delta_y)
            {
                return -1;
            }

            if (x.Rectangle.X - y.Rectangle.X > delta_x) return 1;
            if (x.Rectangle.X - y.Rectangle.X < -delta_x) return -1;

            return 0;
        }
    }
}
