using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkAnalyzerTest
{
    class InkUtils
    {
        public static void Scale(StrokeCollection strokes, double scale)
        {
            foreach (Stroke stroke in strokes)
                for (int i = 0; i < stroke.StylusPoints.Count; i++)
                    // Struct type can't be used as foreach.
                    stroke.StylusPoints[i] = new StylusPoint(
                        stroke.StylusPoints[i].X * scale,
                        stroke.StylusPoints[i].Y * scale);
        }

        public static void Shift(StrokeCollection strokes, double shiftX, double shiftY)
        {
            foreach (Stroke stroke in strokes)
                for (int i = 0; i < stroke.StylusPoints.Count; i++)
                    // Struct type can't be used as foreach.
                    stroke.StylusPoints[i] = new StylusPoint(
                        stroke.StylusPoints[i].X + shiftX,
                        stroke.StylusPoints[i].Y + shiftY);
        }

        public static double StrokeXMin(StrokeCollection strokes)
        {
            double minX = double.MaxValue;
            foreach (Stroke stroke in strokes)
                foreach (StylusPoint point in stroke.StylusPoints)
                    minX = Math.Min(minX, point.X);
            return minX;
        }

        public static double StrokeXRange(StrokeCollection strokes)
        {
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            foreach (Stroke stroke in strokes)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    minX = Math.Min(minX, point.X);
                    maxX = Math.Max(maxX, point.X);
                }
            }
            return maxX - minX;
        }

        public static double StrokeYRange(StrokeCollection strokes)
        {
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            foreach (Stroke stroke in strokes)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    minY = Math.Min(minY, point.Y);
                    maxY = Math.Max(maxY, point.Y);
                }
            }
            return maxY - minY;
        }

        public static void MatchThickness(StrokeCollection reference, StrokeCollection target)
        {
            int countRef = 0;
            double sumRef = 0.0;
            double ssqRef = 0.0;
            foreach (Stroke stroke in reference)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    countRef++;
                    sumRef += point.PressureFactor;
                    ssqRef += point.PressureFactor * point.PressureFactor;
                }
            }

            double avgRef = sumRef / countRef;
            double sdvRef = Math.Sqrt(ssqRef) / countRef;

            int countTarget = 0;
            double sumTarget = 0.0;
            double ssqTarget = 0.0;
            foreach (Stroke stroke in target)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    countTarget++;
                    sumTarget += point.PressureFactor;
                    ssqTarget += point.PressureFactor * point.PressureFactor;
                }
            }

            double avgTarget = sumTarget / countTarget;
            double sdvTarget = Math.Sqrt(ssqTarget) / countTarget;

            // Do bell curve on stylus pressures.
            foreach (Stroke stroke in target)
            {
                for (int i = 0; i < stroke.StylusPoints.Count; i++)
                {
                    StylusPoint point = stroke.StylusPoints[i];
                    float p = point.PressureFactor;
                    if (p != 0.0f)
                    {
                        p = (float)(((p - avgTarget) / sdvTarget) * sdvRef + avgRef);
                        p = Math.Max(0.05f, Math.Min(1.0f, p));
                    }
                    stroke.StylusPoints[i] = new StylusPoint(point.X, point.Y, p);
                }
            }
        }

        public static double distSquared(StylusPoint p1, StylusPoint p2)
        {
            double dx = p1.X - p2.X, dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }

        public static double lineDistSquared(StylusPoint a, StylusPoint b, StylusPoint p)
        {
            double bx = b.X - a.X, by = b.Y - a.Y;
            double px = p.X - a.X, py = p.Y - a.Y;
            double dot = bx * px + by * py;
            double projScale = dot / (bx * bx + by * by);
            if (projScale < 0) projScale = 0;
            else if (projScale > 1) projScale = 1;
            double perpX = px - bx * projScale, perpY = py - by * projScale;
            return perpX * perpX + perpY * perpY;
        }

        public static StylusPointCollection toPolyline(StylusPointCollection points)
        {
            StylusPointCollection r = new StylusPointCollection();
            if (points.Count == 0) return r;
            double totalLength = 0;
            for (int i = 1; i < points.Count; ++i)
            {
                totalLength += Math.Sqrt(distSquared(points[i - 1], points[i]));
            }
            double maxDeviationSquared = Math.Log(totalLength + 150) * 57 - 285;
                //4.8 * Math.Log(totalLength + 30) - 10; //20 * (Math.Log(distSquared(p1, p2) + 1) + 1);
            //Debug.WriteLine("{0} => {2}^2 = {1}", totalLength, maxDeviationSquared, Math.Sqrt(maxDeviationSquared));
            r.Add(points[0]);
            for (int i = 0; i+1 < points.Count;)
            {
                int j = i+2;
                StylusPoint p1 = points[i];
                while (j < points.Count)
                {
                    StylusPoint p2 = points[j];
                    for (int k = i + 1; k < j; ++k)
                    {
                        if (lineDistSquared(p1, p2, points[k]) > maxDeviationSquared)
                        {
                            goto stop;
                        }
                    }
                    ++j;
                }
            stop:
                --j;
                r.Add(points[j]);
                i = j;
            }
            return r;
        }

        public static void transposeStrokes(InkAnalyzer inkAnalyzer, StrokeCollection strokes, double offsetX, double offsetY)
        {
            if(inkAnalyzer != null)
            {
                inkAnalyzer.RemoveStrokes(strokes);
            }

            Matrix transform = new Matrix();
            transform.Translate(offsetX, offsetY);
            strokes.Transform(transform, false);

            if(inkAnalyzer != null)
            {
                inkAnalyzer.AddStrokes(strokes);
            }
        }
    }
}
