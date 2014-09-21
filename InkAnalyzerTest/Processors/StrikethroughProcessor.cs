using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace InkAnalyzerTest.Processors
{
    public class StrikethroughProcessor : InkProcessor
    {
        private InkCanvas canvas;

        public StrikethroughProcessor(InkCanvas canvas)
        {
            this.canvas = canvas;
        }

        public void process(InkAnalyzer inkAnalyzer)
        {
            ContextNodeCollection contextNodeCollection = inkAnalyzer.FindLeafNodes();
            List<Stroke> horizontalLines = InkUtils.findLines(contextNodeCollection);

            findAndDeleteStrikethrough(inkAnalyzer, canvas, horizontalLines, contextNodeCollection);
        }

        private void findAndDeleteStrikethrough(InkAnalyzer inkAnalyzer, InkCanvas canvas,
            List<Stroke> horizontalLines, ContextNodeCollection contextNodeCollection)
        {
            List<ContextNode> deletedNodes = new List<ContextNode>();
            List<Stroke> removedHorizontalLines = new List<Stroke>();

            //Find things to apply gestures to
            foreach (ContextNode node in contextNodeCollection)
            {
                if (node.Strokes.Count == 0)
                {
                    continue;
                }
                Rect strikethroughBounds = node.Strokes.GetBounds();
                strikethroughBounds.Height *= 0.75d;
                if (node is InkWordNode)
                {
                    PointCollection bl = (node as InkWordNode).GetBaseline();
                    double baseline = bl[0].Y;
                    strikethroughBounds.Height = baseline - strikethroughBounds.Y;
                }

                for (int j = 0; j < horizontalLines.Count; j++)
                {
                    if (node.Strokes[0] == horizontalLines[j])
                    {
                        break;
                    }
                    Stroke horizontalLine = horizontalLines[j];
                    Rect horizontalLineBounds = horizontalLine.GetBounds();
                    double sideBuffer = (1 - Constants.LINE_WORD_OVERLAPSE_RATIO) / 2;
                    double strikethroughBoundLeft = strikethroughBounds.X + strikethroughBounds.Width * sideBuffer;
                    double strikethroughBoundRight = strikethroughBounds.X + strikethroughBounds.Width * (1 - sideBuffer);
                    if (strikethroughBounds.IntersectsWith(horizontalLineBounds) &&
                        strikethroughBoundLeft > horizontalLineBounds.X &&
                        strikethroughBoundRight < horizontalLineBounds.X + horizontalLineBounds.Width)
                    {
                        //Delete strikethrough
                        deletedNodes.Add(node);
                        removedHorizontalLines.Add(horizontalLine);
                    }
                }
            }

            foreach (Stroke stroke in removedHorizontalLines)
            {
                horizontalLines.Remove(stroke);
                canvas.Strokes.Remove(stroke);
                inkAnalyzer.RemoveStroke(stroke);
            }

            //Final step to apply the gestures, commit changes
            for (int i = deletedNodes.Count - 1; i >= 0; i--)
            {
                ContextNode node = deletedNodes[i];
                try
                {
                    Rect bounds = node.Strokes.GetBounds();
                    double nodeX = bounds.X;
                    ContextNode parent = node.ParentNode;
                    double closestX = double.MaxValue;
                    foreach (ContextNode sibling in parent.SubNodes)
                    {
                        double siblingX = sibling.Strokes.GetBounds().X;
                        if (siblingX > nodeX && siblingX < closestX)
                        {
                            closestX = siblingX;
                        }
                    }
                    double dx = nodeX - closestX;
                    foreach (ContextNode sibling in parent.SubNodes)
                    {
                        //Nodes right side of current
                        if (sibling.Strokes.GetBounds().X > nodeX)
                        {
                            InkUtils.transposeStrokes(inkAnalyzer, sibling.Strokes, dx, 0d);
                        }
                    }
                    canvas.Strokes.Remove(node.Strokes);
                    inkAnalyzer.RemoveStrokes(node.Strokes);
                }
                catch (Exception e)
                {
                    //Ignore already deleted error
                }
            }
        }

    }
}
