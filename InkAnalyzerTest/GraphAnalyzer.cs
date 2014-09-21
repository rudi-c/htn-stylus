using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkAnalyzerTest
{

    public abstract class Graph
    {
        public interface IDelegate
        {
            void addStroke(Graph g, Stroke s);
        }

        public IDelegate del;
        public Stroke box;
        protected Rect bounds;
        protected InkAnalyzer analyzer;

        public Graph(IDelegate _del, Stroke b)
        {
            del = _del;
            box = b;
            bounds = box.GetBounds();
            box.StylusPoints = InkUtils.xkcd(InkUtils.box(bounds));
            box.DrawingAttributes.Color = Colors.Blue;
            analyzer = new InkAnalyzer();
            analyzer.ContextNodeCreated += ContextNodeCreated;
        }
        protected abstract void ContextNodeCreated(object sender, ContextNodeCreatedEventArgs e);
        public abstract bool takeStroke(Stroke s);
        public bool containsStroke(Stroke s)
        {
            return s.HitTest(bounds, 80);
        }
        public abstract void allStrokes(Action<Stroke> f);
        public abstract void remove(Stroke s);
    }

    public class XYGraph : Graph
    {
        Stroke xAxis, yAxis;
        Point origin, xEnd, yEnd;
        StrokeCollection curves;
        ColorGen colGen;
        public XYGraph(IDelegate del, Stroke b) : base(del, b) {
            curves = new StrokeCollection();
            colGen = new ColorGen();
        }
        public override bool takeStroke(Stroke s)
        {
            InkUtils.depressurize(s.StylusPoints);
            StylusPointCollection col = InkUtils.toPolyline(s.StylusPoints);
            if (xAxis == null || yAxis == null)
            {
                if (col.Count == 2)
                {
                    StylusPoint p1 = col[0];
                    StylusPoint p2 = col[1];
                    if (xAxis == null && Math.Abs(p2.X - p1.X) > 100 && Math.Abs((p2.Y - p1.Y) / (p2.X - p1.X)) < 0.3)
                    {
                        if (yAxis == null)
                        {
                            if (p1.X < p2.X)
                            {
                                origin = new Point(p1.X, p1.Y);
                                xEnd = new Point(p2.X, p1.Y);
                            }
                            else
                            {
                                origin = new Point(p2.X, p2.Y);
                                xEnd = new Point(p1.X, p2.Y);
                            }
                        }
                        else if (InkUtils.distSquared(p2, InkUtils.sp(origin)) < 20 * 20)
                        {
                            xEnd = new Point(p1.X, origin.Y);
                        }
                        else if (InkUtils.distSquared(p1, InkUtils.sp(origin)) < 20 * 20)
                        {
                            xEnd = new Point(p2.X, origin.Y);
                        }
                        else
                        {
                            goto notX;
                        }
                        xAxis = s;
                        s.StylusPoints = InkUtils.xkcd(new StylusPointCollection(new Point[] { origin, xEnd }));
                        return true;
                    }
                notX:
                    if (yAxis == null && Math.Abs(p2.Y - p1.Y) > 100 && Math.Abs((p2.X - p1.X) / (p2.Y - p1.Y)) < 0.3)
                    {
                        if (xAxis == null)
                        {
                            if (p1.Y > p2.Y)
                            {
                                origin = new Point(p1.X, p1.Y);
                                yEnd = new Point(p1.X, p2.Y);
                            }
                            else
                            {
                                origin = new Point(p2.X, p2.Y);
                                yEnd = new Point(p2.X, p1.Y);
                            }
                        }
                        else if (InkUtils.distSquared(p2, InkUtils.sp(origin)) < 20 * 20)
                        {
                            yEnd = new Point(origin.X, p1.Y);
                        }
                        else if (InkUtils.distSquared(p1, InkUtils.sp(origin)) < 20 * 20)
                        {
                            yEnd = new Point(origin.X, p2.Y);
                        }
                        else
                        {
                            goto notY;
                        }
                        yAxis = s;
                        s.StylusPoints = InkUtils.xkcd(new StylusPointCollection(new Point[] { origin, yEnd }));
                        return true;
                    }
                notY: { }
                }
            }
            else
            {
                StylusPoint spOrigin = InkUtils.sp(origin), spXEnd = InkUtils.sp(xEnd), spYEnd = InkUtils.sp(yEnd);
                // Support bars
                if (col.Count == 4
                    // Vertical bars
                    && InkUtils.isVertical(col[0], col[1])
                    //&& InkUtils.isHorizontal(col[1], col[2])
                    && InkUtils.isVertical(col[2], col[3])
                    && InkUtils.similar(Math.Sqrt(InkUtils.distSquared(col[0], col[1])),
                    Math.Sqrt(InkUtils.distSquared(col[2], col[3])))
                    && InkUtils.lineDistSquared(spOrigin, spXEnd, col[0]) < 400
                    && InkUtils.lineDistSquared(spOrigin, spXEnd, col[3]) < 400)
                {
                    double x1 = (col[0].X + col[1].X) / 2;
                    double x2 = (col[2].X + col[3].X) / 2;
                    double y = (col[1].Y + col[2].Y) / 2;
                    s.StylusPoints = InkUtils.xkcd(new StylusPointCollection(new Point[] {
                        new Point(x1, origin.Y),
                        new Point(x1, y),
                        new Point(x2, y),
                        new Point(x2, origin.Y)
                    }));
                    fillBar(x1, origin.Y, x2, y);
                }
                else if (col.Count == 4
                    // Horizontal bars
                    && InkUtils.isHorizontal(col[0], col[1])
                    //&& InkUtils.isVertical(col[1], col[2])
                    && InkUtils.isHorizontal(col[2], col[3])
                    && InkUtils.similar(Math.Sqrt(InkUtils.distSquared(col[0], col[1])),
                    Math.Sqrt(InkUtils.distSquared(col[2], col[3])))
                    && InkUtils.lineDistSquared(spOrigin, spYEnd, col[0]) < 400
                    && InkUtils.lineDistSquared(spOrigin, spYEnd, col[3]) < 400)
                {
                    double y1 = (col[0].Y + col[1].Y) / 2;
                    double y2 = (col[2].Y + col[3].Y) / 2;
                    double x = (col[1].X + col[2].X) / 2;
                    s.StylusPoints = InkUtils.xkcd(new StylusPointCollection(new Point[] {
                        new Point(origin.X, y1),
                        new Point(x, y1),
                        new Point(x, y2),
                        new Point(origin.X, y2)
                    }));
                    fillBar(origin.X, y1, x, y2);
                }
                // Maybe it's inside the graph itself?
                else if (s.HitTest(new Rect(xEnd, yEnd), 80))
                {
                    s.StylusPoints = InkUtils.xkcd(s.StylusPoints);
                }
            }

            curves.Add(s);
//            s.StylusPoints = InkUtils.xkcd(col);

            //analyzer.AddStroke(s);
            //analyzer.Analyze();
            return true;
        }
        protected override void ContextNodeCreated(object sender, ContextNodeCreatedEventArgs e)
        {
            Debug.WriteLine("Holy molly! {0}", e.NodeCreated);
            InkDrawingNode n = e.NodeCreated as InkDrawingNode;
            if (n != null)
            {
                Debug.WriteLine("Yeah!");
                foreach (Stroke x in n.Strokes)
                {
                    Debug.WriteLine("Uhhhhh {0}", x);
                    analyzer.RemoveStroke(x);
                    x.StylusPoints = InkUtils.xkcd(x.StylusPoints);
                }
            }
        }
        public override void allStrokes(Action<Stroke> f)
        {
            if (xAxis != null) f(xAxis);
            if (yAxis != null) f(yAxis);
            foreach (Stroke s in curves)
            {
                f(s);
            }
        }
        public override void remove(Stroke s)
        {
            if (s == xAxis)
            {
                xAxis = null;
            }
            else if (s == yAxis)
            {
                yAxis = null;
            }
            else
            {
                curves.Remove(s);
            }
        }
        private void fillBar(double _x1, double _y1, double _x2, double _y2)
        {
            const double dist = 14, stdev = 4, fillGap = 0.05;
            double x1 = Math.Min(_x1, _x2),
                x2 = Math.Max(_x1, _x2),
                y1 = Math.Min(_y1, _y2),
                y2 = Math.Max(_y1, _y2);
            double d = x1 + y1 + dist;
            StylusPointCollection r = new StylusPointCollection();
            //r.Add(new StylusPoint(x1, y1));
            bool side = false;
            while (d < x2 + y2 - dist)
            {
                if (side = !side)
                {
                    double x1_ = x1 + (x2 - x1) * Math.Abs(InkUtils.stdNorm() * fillGap);
                    double y2_ = y2 - (y2 - y1) * Math.Abs(InkUtils.stdNorm() * fillGap);
                    if (d - x1_ < y2_)
                    {
                        r.Add(new StylusPoint(x1_, d - x1_));
                    }
                    else
                    {
                        r.Add(new StylusPoint(d - y2_, y2_));
                    }
                }
                else
                {
                    double x2_ = x2 - (x2 - x1) * Math.Abs(InkUtils.stdNorm() * fillGap);
                    double y1_ = y1 + (y2 - y1) * Math.Abs(InkUtils.stdNorm() * fillGap);
                    if (d - x2_ > y1_)
                    {
                        r.Add(new StylusPoint(x2_, d - x2_));
                    }
                    else
                    {
                        r.Add(new StylusPoint(d - y1_, y1_));
                    }
                    d += Math.Abs(InkUtils.stdNorm() * stdev + dist);
                }
            }
            Stroke s = new Stroke(InkUtils.xkcd(r));
            s.DrawingAttributes.Color = colGen.nextColor();
            addStroke(s);
        }

        private void addStroke(Stroke s)
        {
            del.addStroke(this, s);
        }
    }

    public class GraphAnalyzer : Graph.IDelegate
    {
        MainWindow mainWindow;
        InkAnalyzer analyzer;
        HashSet<Graph> graphs;
        Dictionary<Stroke, Graph> strokes;
        public GraphAnalyzer(MainWindow m)
        {
            mainWindow = m;
            analyzer = new InkAnalyzer();
            graphs = new HashSet<Graph>();
            strokes = new Dictionary<Stroke, Graph>();
        }

        /// <summary>
        /// Analyze a new stroke.
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns>true if the stroke was recognized as belonging to a graph (and so should be excluded from InkAnalyzer)</returns>
        public bool newStroke(Stroke stroke)
        {
            if (strokes.ContainsKey(stroke))
            {
                // already have it!
                return true;
            }
            foreach (Graph g in graphs)
            {
                if (g.containsStroke(stroke) && g.takeStroke(stroke))
                {
                    strokes.Add(stroke, g);
                    return true;
                }
            }
            Stroke copy = stroke.Clone();
            analyzer.AddStroke(copy);

            analyzer.Analyze();

            StrokeCollection sc = new StrokeCollection(new Stroke[] { copy });
            ContextNodeCollection ctxNodes = analyzer.FindInkLeafNodes(sc);
            foreach (ContextNode ctxNode in ctxNodes)
            {
                if (ctxNode is InkDrawingNode && (ctxNode as InkDrawingNode).GetShapeName() == "Rectangle")
                {
                    Graph g = new XYGraph(this, stroke);
                    graphs.Add(g);
                    strokes.Add(stroke, g);
                    analyzer.RemoveStroke(copy);
                    return true;
                }
            }
            analyzer.RemoveStroke(copy);
            return false;
        }

        public void removeStroke(Stroke stroke)
        {
            if (!strokes.ContainsKey(stroke))
            {
                return;
            }
            Graph g = strokes[stroke];
            if (g.box == stroke)
            {
                // destroy the whole thing
                g.allStrokes(s => strokes.Remove(s));
                graphs.Remove(g);
            }
            else
            {
                g.remove(stroke);
            }
        }

        public void addStroke(Graph g, Stroke s)
        {
            strokes.Add(s, g);
            mainWindow.MainInkCanvas.Strokes.Add(s);
        }
    }

}