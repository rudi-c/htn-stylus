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

        private bool strokeIsCaret(Stroke oldStrokes)
        {
            StylusPointCollection styluses = InkUtils.toPolyline(oldStrokes.StylusPoints);

            if (styluses.Count >= 5)
            {
                bool hasGoodFirst = false;
                bool hasGoodSecond = false;
                bool hasGoodThird = false;
                bool hasGoodFourth = false;
                StylusPoint point0 = styluses.First();
                StylusPoint point1 = styluses[styluses.Count / 4];
                StylusPoint point2 = styluses[styluses.Count / 2];
                StylusPoint point3 = styluses[styluses.Count * 3 / 4];
                StylusPoint point4 = styluses.Last();
                double firstOffset = 0;
                double secondOffset = 0;
                double thirdOffset = 0;
                double fourthOffset = 0;
                double firstExpectedSlope = 0;
                double secondExpectedSlope = 0;
                if (point0.X < point1.X)
                {
                    firstExpectedSlope = (point1.Y - point0.Y) / (point1.X - point0.X);
                    if (firstExpectedSlope < -0.5 && firstExpectedSlope > -4)
                    {
                        double sum = 0;
                        for (int i = 0; i < styluses.Count / 4; i++)
                        {
                            StylusPoint point = styluses[i];
                            double expectedY = point0.Y + (point.X - point0.X) * firstExpectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        firstOffset = sum / styluses.Count();
                        if (firstOffset < 50)
                        {
                            hasGoodFirst = true;
                        }
                    }
                }
                if (point1.X < point2.X)
                {
                    secondExpectedSlope = (point2.Y - point1.Y) / (point2.X - point1.X);
                    if (secondExpectedSlope > 0.5 && secondExpectedSlope < 4)
                    {
                        double sum = 0;
                        for (int i = styluses.Count / 4; i < styluses.Count / 2; i++)
                        {
                            StylusPoint point = styluses[i];
                            double expectedY = point1.Y + (point.X - point1.X) * secondExpectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        secondOffset = sum / styluses.Count();

                        if (secondOffset < 50)
                        {
                            hasGoodSecond = true;
                        }
                    }
                }
                if (point3.X < point2.X)
                {
                    double retraceExpectedSlope =  (point3.Y - point2.Y) / (point3.X - point2.X);
                    if (retraceExpectedSlope > 0.5 && retraceExpectedSlope < 4 && Math.Abs(retraceExpectedSlope - secondExpectedSlope) > 0.1)
                    {
                        double sum = 0;
                        for (int i = styluses.Count / 2; i < 3 * styluses.Count / 4; i++)
                        {
                            StylusPoint point = styluses[i];
                            double expectedY = point2.Y + (point.X - point2.X) * secondExpectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        thirdOffset = sum / styluses.Count();

                        if (thirdOffset < 50)
                        {
                            hasGoodThird = true;
                        }
                    }
                }
                if (point4.X < point3.X)
                {
                    double retraceExpectedSlope = (point4.Y - point3.Y) / (point4.X - point3.X);
                    if (firstExpectedSlope < -0.5 && firstExpectedSlope > -4 && Math.Abs(retraceExpectedSlope - firstExpectedSlope) > 0.1)
                    {
                        double sum = 0;
                        for (int i = 3 * styluses.Count / 4; i < styluses.Count; i++)
                        {
                            StylusPoint point = styluses[i];
                            double expectedY = point3.Y + (point.X - point3.X) * firstExpectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        fourthOffset = sum / styluses.Count();
                        if (fourthOffset < 50)
                        {
                            hasGoodFourth = true;
                        }
                    }
                }
                //if (hasGoodFirst && hasGoodSecond && hasGoodThird && hasGoodFourth)
               // {
                    Debug.WriteLine(firstOffset + " " + secondOffset + " "  + thirdOffset + " " + fourthOffset);
                //}
                return hasGoodFirst && hasGoodSecond && hasGoodThird && hasGoodFourth;
            }
            return false;
        }
    }
}
