using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

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
            List<StrokeCollection> deletedStrokes = new List<StrokeCollection>();
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
                    double baseline = (node as InkWordNode).GetBaseline()[0].Y;
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
                    if (strikethroughBounds.IntersectsWith(horizontalLineBounds))
                    {
                        //Delete strikethrough
                        deletedStrokes.Add(node.Strokes);
                        StrokeCollection singleStroke = new StrokeCollection();
                        singleStroke.Add(horizontalLine);
                        deletedStrokes.Add(singleStroke);

                        removedHorizontalLines.Add(horizontalLine);
                    }
                }
            }

            foreach (Stroke stroke in removedHorizontalLines)
            {
                horizontalLines.Remove(stroke);
            }

            //Final step to apply the gestures, commit changes
            foreach (StrokeCollection stroke in deletedStrokes)
            {
                try
                {
                    canvas.Strokes.Remove(stroke);
                    inkAnalyzer.RemoveStrokes(stroke);
                }
                catch (Exception e)
                {
                    //Ignore already deleted error
                }
            }
        }

    }
}
