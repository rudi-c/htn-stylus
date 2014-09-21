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
    class CorrRandExp
    {
        double stdev, preserve;
        double prev;
        public CorrRandExp(double _stdev, double _preserve)
        {
            stdev = _stdev; preserve = _preserve;
            prev = 0;
        }
        public double next()
        {
            prev = prev * preserve + InkUtils.stdNorm() * stdev;
            return prev;
        }
    }
    class CorrRandWindow {
        double stdev;
        int i, window;
        double[] buffer;
        double prev;
        public CorrRandWindow(double _stdev, int _window)
        {
            stdev = _stdev;
            window = _window;
            i = 0;
            buffer = new double[window];
            prev = InkUtils.stdNorm() * stdev * Math.Sqrt(window);
        }
        public double next()
        {
            prev = prev + (buffer[(i + 1) % window] = InkUtils.stdNorm() * stdev) - buffer[i % window];
            ++i;
            return prev;
        }
    }
    class InkUtils
    {
        public static void Scale(StrokeCollection strokes, double scale)
        {
            foreach (Stroke stroke in strokes)
                for (int i = 0; i < stroke.StylusPoints.Count; i++)
                    // Struct type can't be used as foreach.
                    stroke.StylusPoints[i] = new StylusPoint(
                        stroke.StylusPoints[i].X * scale,
                        stroke.StylusPoints[i].Y * scale,
                        stroke.StylusPoints[i].PressureFactor);
        }

        public static void Shift(StrokeCollection strokes, double shiftX, double shiftY)
        {
            foreach (Stroke stroke in strokes)
                for (int i = 0; i < stroke.StylusPoints.Count; i++)
                    // Struct type can't be used as foreach.
                    stroke.StylusPoints[i] = new StylusPoint(
                        stroke.StylusPoints[i].X + shiftX,
                        stroke.StylusPoints[i].Y + shiftY,
                        stroke.StylusPoints[i].PressureFactor);
        }

        public static double StrokeXMin(StrokeCollection strokes)
        {
            double minX = double.MaxValue;
            foreach (Stroke stroke in strokes)
                foreach (StylusPoint point in stroke.StylusPoints)
                    minX = Math.Min(minX, point.X);
            return minX;
        }

        public static double StrokeYMax(StrokeCollection strokes)
        {
            double maxY = double.MinValue;
            foreach (Stroke stroke in strokes)
                foreach (StylusPoint point in stroke.StylusPoints)
                    maxY = Math.Max(maxY, point.Y);
            return maxY;
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

        public static StylusPointCollection toPolyline(StylusPointCollection points_)
        {
            const int separation = 3;
            if (points_.Count <= 1) return new StylusPointCollection(points_);
            StylusPointCollection points = resample(points_, 3);
            StylusPointCollection r = new StylusPointCollection();
            r.Add(points[0]);
            int i;
            for (i = separation; i + separation < points.Count; ++i)
            {
                StylusPoint prev = points[i - separation], cur = points[i], next = points[i + separation];
                double dx1 = cur.X - prev.X, dy1 = cur.Y - prev.Y;
                double n1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                dx1 /= n1; dy1 /= n1;
                double dx2 = next.X - cur.X, dy2 = next.Y - cur.Y;
                double n2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                dx2 /= n2; dy2 /= n2;
                if (dx1 * dx2 + dy1 * dy2 < 0.7)
                {
                    if (i < separation*2)
                    {
                        r.RemoveAt(0);
                    }
                    r.Add(points[i]);
                    i = i + separation * 2 - 1;
                }
            }
            if (i + separation == points.Count)
            {
                r.Add(points[points.Count - 1]);
            }
            return r;
        }
        public static StylusPointCollection toPolyline2(StylusPointCollection points)
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

        public static List<Stroke> findLines(ContextNodeCollection contextNodeCollection)
        {
            List<Stroke> horizontalLines = new List<Stroke>();

            //Find single line gestures
            foreach (ContextNode node in contextNodeCollection)
            {
                if (node.Strokes.Count == 1)
                {
                    Stroke stroke = node.Strokes[0];
                    if (strokeIsHorizontalLine(stroke))
                    {
                        horizontalLines.Add(stroke);
                    }
                }
            }

            //Find single line in words
            foreach (ContextNode node in contextNodeCollection)
            {
                if (node.Strokes.Count == 0)
                {
                    continue;
                }
                if (node is InkWordNode)
                {
                    InkWordNode word = node as InkWordNode;
                    Rect bounds = word.Strokes.GetBounds();
                    foreach (Stroke stroke in word.Strokes)
                    {
                        if (stroke.GetBounds().Width / bounds.Width >= 0.9d)
                        {
                            horizontalLines.Add(stroke);
                        }
                    }
                }
            }

            return horizontalLines;
        }

        private static bool strokeIsHorizontalLine(Stroke stroke)
        {
            Rect bounds = stroke.GetBounds();
            return bounds.Height / bounds.Width < 0.1;
        }

        public static StylusPoint sp(Point p)
        {
            return new StylusPoint(p.X, p.Y);
        }

        public static StylusPointCollection box(Rect rect)
        {
            StylusPointCollection r = new StylusPointCollection();
            r.Add(sp(rect.TopLeft));
            r.Add(sp(rect.TopRight));
            r.Add(sp(rect.BottomRight));
            r.Add(sp(rect.BottomLeft));
            r.Add(sp(rect.TopLeft));
            return r;
        }

        static bool stdNormState = false;
        static double stdNormNextResult = 0;
        static Random stdNormRandom = new Random();
        /// <summary>
        /// Generates a random number according to the standard normal distribution.
        /// </summary>
        /// <returns>the number</returns>
        public static double stdNorm()
        {
            if (stdNormState)
            {
                stdNormState = false;
                return stdNormNextResult;
            }
            double r = Math.Sqrt(-2*Math.Log(1 - stdNormRandom.NextDouble()));
            double theta = 2*Math.PI*stdNormRandom.NextDouble();
            stdNormNextResult = r * Math.Cos(theta);
            return r * Math.Sin(theta);
        }

        public static StylusPointCollection resample(StylusPointCollection pts, double spacing = 2)
        {
            if (pts.Count <= 1) return new StylusPointCollection(pts);
            StylusPointCollection r = new StylusPointCollection();
            int i = 0;
            StylusPoint cur = pts[i], next = pts[i + 1];
            double dist = Math.Sqrt(distSquared(cur, next));
            double offset = 0;
            while (i + 1 < pts.Count)
            {
                if (offset >= dist)
                {
                    offset -= dist;
                    ++i;
                    cur = next;
                    if (i + 1 >= pts.Count)
                    {
                        break;
                    }
                    next = pts[i + 1];
                    dist = Math.Sqrt(distSquared(cur, next));
                    continue;
                }
                double dX = next.X - cur.X, dY = next.Y - cur.Y;
                float dP = next.PressureFactor - cur.PressureFactor;
                double factor = offset / dist;
                r.Add(new StylusPoint(
                    cur.X + factor * dX,
                    cur.Y + factor * dY,
                    cur.PressureFactor + (float)factor * dP
                    ));
                offset += spacing;
            }
            r.Add(cur);
            return r;
        }

        public static StylusPointCollection xkcd(StylusPointCollection pts)
        {
            StylusPointCollection r = resample(pts);
            if (r.Count < 2) return r;
            StylusPoint prev = r[0];
            CorrRandExp r1 = new CorrRandExp(0.15, 0.99);
            CorrRandWindow r2 = new CorrRandWindow(0.1, 20);
            for (int i = 1; i+1 < r.Count; ++i)
            {
                StylusPoint cur = r[i], next = r[i + 1];
                double dX = next.X - prev.X, dY = next.Y - prev.Y;
                double sqrNorm = dX * dX + dY * dY;
                if (sqrNorm == 0)
                {
                    continue;
                }
                double amt = r1.next();// +r2.next();
                double factor = amt / Math.Sqrt(sqrNorm);
                prev = cur;
                cur.X -= factor * dY;
                cur.Y += factor * dX;
                r[i] = cur;
            }
            return r;
        }

        public static void depressurize(StylusPointCollection col)
        {
            for (int i = 0; i < col.Count; ++i)
            {
                col[i] = new StylusPoint(
                    col[i].X,
                    col[i].Y
                    );
            }
        }
        public static bool isVertical(StylusPoint a, StylusPoint b)
        {
            return Math.Abs((b.X - a.X) / (b.Y - a.Y)) < 0.7;
        }
        public static bool isHorizontal(StylusPoint a, StylusPoint b)
        {
            return Math.Abs((b.Y - a.Y) / (b.X - a.X)) < 0.7;
        }
        public static bool similar(double a, double b)
        {
            return Math.Abs(a - b) < Math.Log(Math.Max(a, b)) * 3.5 - 5;
        }
    }
}
