using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkAnalyzerTest.Processors
{
    public class InsertionProcessor : InkProcessor
    {
        Stroke strokeToBeReplaced;
        InkCanvas mainInkCanvas;
        InsertionBox insertionBox;

        public InsertionProcessor(InkCanvas mainInkCanvas, InsertionBox insertionBox)
        {
            this.mainInkCanvas = mainInkCanvas;
            this.insertionBox = insertionBox;
        }

        public void process(InkAnalyzer inkAnalyzer)
        {
            ContextNodeCollection nodeCollection = inkAnalyzer.FindLeafNodes();
            foreach (ContextNode childNodes in nodeCollection)
            {
                foreach (Stroke stroke in childNodes.Strokes)
                {
                    if (strokeIsCaret(stroke))
                    {
                        insertionBox.Visibility = Visibility.Visible;
                        Canvas.SetLeft(insertionBox, stroke.StylusPoints[0].X - 140);
                        Canvas.SetTop(insertionBox, stroke.StylusPoints[1].Y);
                        strokeToBeReplaced = stroke;
                    }
                }
            }
        }

        public void insertStrokes(InkAnalyzer analyzer, InkCanvas mainInkCanvas, InkCanvas insertionCanvas)
        {
            double bestY = -10000;
            ContextNode selectedNode = null;
            foreach (ContextNode node in analyzer.FindLeafNodes())
            {
                double y1 = strokeToBeReplaced.GetBounds().Y;
                double y2 = node.Strokes.GetBounds().Y;
                if (y1 - y2 > 0 && y1 - y2 < y1 - bestY)
                {
                    bestY = y2;
                    selectedNode = node;
                }
            }
            if (bestY == -10000)
            {
                bestY = strokeToBeReplaced.GetBounds().Y;
            }
            StrokeCollection strokeCollection = insertionCanvas.Strokes.Clone();
            mainInkCanvas.Strokes.Add(strokeCollection);
            double bestX = strokeToBeReplaced.GetBounds().X;
            double strokeX = strokeCollection.GetBounds().X;
            double strokeY = strokeCollection.GetBounds().Y;
            Matrix inkTransform = new Matrix();
            inkTransform.Translate(bestX - strokeX + 20, bestY - strokeY);
            strokeCollection.Transform(inkTransform, false);
            if (selectedNode != null) {
                for (int i = 0; i < selectedNode.ParentNode.SubNodes.Count; i++)
                {
                    ContextNode siblingNode = selectedNode.ParentNode.SubNodes[i];
                    double width = strokeCollection.GetBounds().Width;
                    double startX = strokeCollection.GetBounds().X;
                    for (int j = 0; j < siblingNode.Strokes.Count; j++)
                    {
                        Stroke stroke = siblingNode.Strokes[j];
                        double offsetX = stroke.GetBounds().X;
                        
                        if (offsetX - startX > 0)
                        {
                            Matrix transform = new Matrix();
                            transform.Translate(width, 0);
                            stroke.Transform(transform, false);
                        } 
                    }
                }
            }            
            insertionCanvas.Strokes.Clear();
            mainInkCanvas.Strokes.Remove(strokeToBeReplaced);
        }

        public void cancel(InkCanvas mainInkCanvas, InkCanvas insertionCanvas)
        {
            insertionCanvas.Strokes.Clear();
            mainInkCanvas.Strokes.Remove(strokeToBeReplaced);
        }

        private bool strokeIsCaret(Stroke oldStrokes)
        {
            StylusPointCollection styluses = InkUtils.toPolyline(oldStrokes.StylusPoints);

            if (styluses.Count == 5)
            {
                double distance1 = (Math.Sqrt(InkUtils.distSquared(styluses[0], styluses[4])));
                double distance2 = (Math.Sqrt(InkUtils.distSquared(styluses[1], styluses[3])));
                bool closeFirst =  distance1 < 24;
                bool closeSecond = distance2 < 24;

                double offset = (styluses[0].Y - styluses[1].Y) / (styluses[0].X - styluses[1].X);
                double levelOffset = (styluses[0].Y - styluses[2].Y);
                bool isHigh = -offset > 1.5;
                bool level = Math.Abs(levelOffset) < 50;

                Debug.Print(distance1 + " " + distance2 + " " + offset + " " + levelOffset);
                return closeFirst && closeSecond && isHigh && level;
            }
            return false;
        }
    }
}
