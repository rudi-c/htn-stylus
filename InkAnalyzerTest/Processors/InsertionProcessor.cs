﻿using System;
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
            double bestY = 10000;
            foreach (ContextNode node in analyzer.FindLeafNodes())
            {
                double y1 = strokeToBeReplaced.GetBounds().Y;
                double y2 = node.Strokes.GetBounds().Y;
                if (y2 - y1 > 0 && y2 - y1 < bestY - y1)
                {
                    bestY = y2;
                }
            }
            StrokeCollection strokes = insertionCanvas.Strokes.Clone();
            mainInkCanvas.Strokes.Add(strokes);
            foreach (Stroke stroke in strokes)
            {
                Matrix inkTransform = new Matrix();
                double bestX = strokeToBeReplaced.GetBounds().X;
                inkTransform.Translate(bestX, bestY);
                stroke.Transform(inkTransform, false);
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
