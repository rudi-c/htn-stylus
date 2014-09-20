using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InkAnalyzerTest
{
    public class CanvasEditor
    {
        public CanvasEditor() { }

        public void analyzeStrokeEvent(InkAnalyzer inkAnalyzer, InkCanvas mainInkCanvas) {
            ContextNodeCollection contextNodeCollection = inkAnalyzer.FindLeafNodes();
            List<ContextNode> crossNodes = new List<ContextNode>();
            for (int i = 0; i < contextNodeCollection.Count; i++)
            {
                ContextNode node = contextNodeCollection[i];
                if (node.Strokes.Count == 1)
                {
                    Stroke stroke = node.Strokes[0];
                    if (strokeIsHorizontalLine(stroke))
                    {
                        crossNodes.Add(node);
                    }
                }
            }
            for (int i = 0; i < contextNodeCollection.Count; i++)
            {
                ContextNode node = contextNodeCollection[i];
                Rect rect = node.Strokes.GetBounds();
                Rect crossRect = new Rect(rect.X, rect.Y, rect.Width, rect.Height * 0.75);
                for (int j = 0; j < crossNodes.Count; j++)
                {
                    ContextNode crossNode = crossNodes[j];
                    if (crossRect.IntersectsWith(crossNode.Strokes.GetBounds()))
                    {
                        mainInkCanvas.Strokes.Remove(node.Strokes);
                    }
                }
            }
        }

        private bool strokeIsHorizontalLine(Stroke stroke)
        {
            if (stroke.StylusPoints.Count() > 2)
            {
                StylusPoint beginningPoint = stroke.StylusPoints.First();
                StylusPoint endPoint = stroke.StylusPoints.Last();
                if (beginningPoint.X < endPoint.X) {
                    double expectedSlope = (endPoint.Y - beginningPoint.Y) / (endPoint.X - beginningPoint.X);
                    if (Math.Abs(expectedSlope) < 0.5)
                    {
                        double sum = 0;
                        foreach (StylusPoint point in stroke.StylusPoints)
                        {
                            double expectedY = beginningPoint.Y + (point.X - beginningPoint.X) * expectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        double offset = sum / stroke.StylusPoints.Count();
                        if (offset < 50)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
