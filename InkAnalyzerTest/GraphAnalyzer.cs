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
        public Stroke box;
        protected Rect bounds;
        protected InkAnalyzer analyzer;

        public Graph(Stroke b)
        {
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
    }

    public class XYGraph : Graph
    {
        Stroke xAxis, yAxis;
        Point origin, xEnd, yEnd;
        StrokeCollection curves;
        public XYGraph(Stroke b) : base(b) { }
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
                }
                // Maybe it's inside the graph itself?
                else if (s.HitTest(new Rect(xEnd, yEnd), 80))
                {
                    s.StylusPoints = InkUtils.xkcd(s.StylusPoints);
                }
            }
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
    }

    public class GraphAnalyzer
    {
        InkAnalyzer analyzer;
        HashSet<Graph> graphs;
        public GraphAnalyzer()
        {
            analyzer = new InkAnalyzer();
            graphs = new HashSet<Graph>();
        }

        /// <summary>
        /// Analyze a new stroke.
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns>true if the stroke was recognized as belonging to a graph (and so should be excluded from InkAnalyzer)</returns>
        public bool newStroke(Stroke stroke)
        {
            foreach (Graph g in graphs)
            {
                if (g.containsStroke(stroke) && g.takeStroke(stroke)) return true;
            }
            Stroke copy = stroke.Clone();
            analyzer.AddStroke(copy);

            // TODO: THIS MIGHT NEED FIXING
            //analyzer.Analyze();

            StrokeCollection sc = new StrokeCollection(new Stroke[] { copy });
            ContextNodeCollection ctxNodes = analyzer.FindInkLeafNodes(sc);
            foreach (ContextNode ctxNode in ctxNodes)
            {
                if (ctxNode is InkDrawingNode && (ctxNode as InkDrawingNode).GetShapeName() == "Rectangle")
                {
                    graphs.Add(new XYGraph(stroke));
                    analyzer.RemoveStroke(copy);
                    return true;
                }
            }
            analyzer.RemoveStroke(copy);
            return false;
        }

        public void removeStroke(Stroke stroke)
        {
            graphs.RemoveWhere(g => stroke == g.box);
        }
    }

}