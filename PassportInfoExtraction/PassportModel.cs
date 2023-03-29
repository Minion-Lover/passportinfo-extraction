using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yolov5Net.Scorer;
using Yolov5Net.Scorer.Models.Abstract;

namespace PassportInfoExtraction
{
    internal class PassportModel : YoloModel
    {
        public override int Width { get; set; } = 640;
        public override int Height { get; set; } = 640;
        public override int Depth { get; set; } = 3;

        public override int Dimensions { get; set; } = 9;

        public override int[] Strides { get; set; } = new int[] { 8, 16, 32, 64 };

        public override int[][][] Anchors { get; set; } = new int[][][]
        {
            new int[][] { new int[] { 019, 027 }, new int[] { 044, 040 }, new int[] { 038, 094 } },
            new int[][] { new int[] { 096, 068 }, new int[] { 086, 152 }, new int[] { 180, 137 } },
            new int[][] { new int[] { 140, 301 }, new int[] { 303, 264 }, new int[] { 238, 542 } },
            new int[][] { new int[] { 436, 615 }, new int[] { 739, 380 }, new int[] { 925, 792 } }
        };

        public override int[] Shapes { get; set; } = new int[] { 160, 80, 40, 20 };

        public override float Confidence { get; set; } = 0.10f;
        public override float MulConfidence { get; set; } = 0.15f;
        public override float Overlap { get; set; } = 0.25f;

        public override string[] Outputs { get; set; } = new[] { "output" };

        public override List<YoloLabel> Labels { get; set; } = new List<YoloLabel>()
        {
            new YoloLabel { Id = 1, Name = "bottom" },
            new YoloLabel { Id = 2, Name = "data" },
            new YoloLabel { Id = 3, Name = "photo" },
            new YoloLabel { Id = 4, Name = "sign" }
        };

        public override bool UseDetect { get; set; } = true;

        public PassportModel()
        {

        }
    }
}
