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
        InkCanvas inkInsertionCanvas;
        Border InkInsertionCanvasParent;
        Button insertionButton;

        public InsertionProcessor(InkCanvas mainInkCanvas, InkCanvas inkInsertionCanvas,
            Border InkInsertionCanvasParent, Button insertionButton)
        {
            this.mainInkCanvas = mainInkCanvas;
            this.inkInsertionCanvas = inkInsertionCanvas;
            this.InkInsertionCanvasParent = InkInsertionCanvasParent;
            this.insertionButton = insertionButton;
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
                        InkInsertionCanvasParent.Visibility = Visibility.Visible;
                        insertionButton.Visibility = Visibility.Visible;
                        Canvas.SetLeft(InkInsertionCanvasParent, stroke.StylusPoints[0].X - 140);
                        Canvas.SetTop(InkInsertionCanvasParent, stroke.StylusPoints[1].Y);
                        Canvas.SetLeft(insertionButton, stroke.StylusPoints[0].X + 20);
                        Canvas.SetTop(insertionButton, stroke.StylusPoints[1].Y + 120);
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

        private bool strokeIsCaret(Stroke stroke)
        {
            if (stroke.StylusPoints.Count() > 3)
            {
                bool hasGoodFirst = false;
                bool hasGoodSecond = false;
                StylusPoint beginningPoint = stroke.StylusPoints.First();
                StylusPoint middlePoint = stroke.StylusPoints[stroke.StylusPoints.Count / 2];
                StylusPoint endPoint = stroke.StylusPoints.Last();
                double firstOffset = 0;
                double secondOffset = 0;
                if (beginningPoint.X < middlePoint.X)
                {
                    double firstExpectedSlope = (middlePoint.Y - beginningPoint.Y) / (middlePoint.X - beginningPoint.X);
                    if (firstExpectedSlope < -0.5 && firstExpectedSlope > -4)
                    {
                        double sum = 0;
                        for (int i = 0; i < stroke.StylusPoints.Count / 2; i++)
                        {
                            StylusPoint point = stroke.StylusPoints[i];
                            double expectedY = beginningPoint.Y + (point.X - beginningPoint.X) * firstExpectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        firstOffset = sum / stroke.StylusPoints.Count();
                        if (firstOffset < 50)
                        {
                            hasGoodFirst = true;
                        }
                    }
                }
                if (middlePoint.X < endPoint.X)
                {
                    double secondExpectedSlope = (endPoint.Y - middlePoint.Y) / (endPoint.X - middlePoint.X);
                    if (secondExpectedSlope > 0.5 && secondExpectedSlope < 4)
                    {
                        double sum = 0;
                        for (int i = stroke.StylusPoints.Count / 2; i < stroke.StylusPoints.Count; i++)
                        {
                            StylusPoint point = stroke.StylusPoints[i];
                            double expectedY = middlePoint.Y + (point.X - middlePoint.X) * secondExpectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        secondOffset = sum / stroke.StylusPoints.Count();

                        if (secondOffset < 50)
                        {
                            hasGoodSecond = true;
                        }
                    }
                }
                if (hasGoodFirst && hasGoodSecond)
                {
                    Debug.WriteLine(firstOffset + " " + secondOffset);
                }
                return hasGoodFirst && hasGoodSecond;
            }
            return false;
        }
    }
}
