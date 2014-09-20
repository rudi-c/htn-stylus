using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;

namespace InkAnalyzerTest
{
    class InkUtils
    {
        public static StrokeCollection Scale(StrokeCollection strokes, double scale)
        {
            foreach (Stroke stroke in strokes)
                for (int i = 0; i < stroke.StylusPoints.Count; i++)
                    // Struct type can't be used as foreach.
                    stroke.StylusPoints[i] = new StylusPoint(
                        stroke.StylusPoints[i].X * scale,
                        stroke.StylusPoints[i].Y * scale);
        }
    }
}
