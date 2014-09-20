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
using System.Drawing;

namespace InkAnalyzerTest
{
    public class CanvasEditor
    {
        InkAnalyzer inkAnalyzer;

        public CanvasEditor() { }

        public void analyzeStrokeEvent(InkAnalyzer inkAnalyzer, InkCanvas mainInkCanvas, Headings mainHeading)
        {
            this.inkAnalyzer = inkAnalyzer;
            ContextNodeCollection contextNodeCollection = inkAnalyzer.FindLeafNodes();
            List<ContextNode> horizontalLines = new List<ContextNode>();
            List<ContextNode> deletedNodes = new List<ContextNode>();
            //Find single line gestures
            foreach(ContextNode node in contextNodeCollection)
            {
                if(node.Strokes.Count == 1)
                {
                    Stroke stroke = node.Strokes[0];
                    //strokeIsCaret(stroke);
                    if(strokeIsHorizontalLine(stroke))
                    {
                        horizontalLines.Add(node);
                    }
                }
            }
            List<HeadingItem> headings = new List<HeadingItem>();
            //Preprocess lines to check for headings
            foreach(ContextNode node1 in horizontalLines)
            {
                List<HeadingItem> intersectHeadings = new List<HeadingItem>();
                foreach(HeadingItem heading in headings)
                {
                    bool intersectsAny = false;
                    foreach(ContextNode node2 in heading.lines)
                    {
                        Rect first = node1.Strokes.GetBounds();
                        Rect second = node2.Strokes.GetBounds();
                        if(Math.Abs(first.Y - second.Y) < 20)
                        {
                            intersectsAny = true;
                            break;
                        }
                    }
                    if(intersectsAny)
                    {
                        intersectHeadings.Add(heading);
                    }
                }

                HeadingItem resultHeading = new HeadingItem();
                foreach(HeadingItem heading in intersectHeadings)
                {
                    resultHeading.lines.AddRange(heading.lines);
                    headings.Remove(heading);
                }
                resultHeading.lines.Add(node1);
                headings.Add(resultHeading);
            }

            //Find things to apply gestures to
            foreach(ContextNode node in contextNodeCollection)
            {
                Rect strikethroughBounds = node.Strokes.GetBounds();
                strikethroughBounds.Height *= 0.75d;
                for(int j = 0; j < horizontalLines.Count; j++)
                {
                    if(node == horizontalLines[j])
                    {
                        break;
                    }
                    ContextNode horizontalLine = horizontalLines[j];
                    Rect horizontalLineBounds = horizontalLine.Strokes.GetBounds();
                    if(strikethroughBounds.IntersectsWith(horizontalLineBounds))
                    {
                        //Delete strikethrough
                        deletedNodes.Add(node);
                        deletedNodes.Add(horizontalLine);
                    }
                }

                Rect underlineBounds = node.Strokes.GetBounds();
                underlineBounds.Y += underlineBounds.Height * 0.75d;
                if(node is InkWordNode)
                {
                    InkWordNode word = node as InkWordNode;

                    foreach(HeadingItem heading in headings)
                    {
                        if(heading.intersects(underlineBounds))
                        {
                            heading.text.Add(word);
                            break;
                        }
                    }
                }
            }

            //Remove bad headings
            List<HeadingItem> actualHeadings = new List<HeadingItem>();
            foreach(HeadingItem heading in headings)
            {
                if(heading.text.Count > 0)
                {
                    actualHeadings.Add(heading);
                }
            }

            //Here is the end result of the headings
            mainHeading.headings = actualHeadings;
            mainHeading.invalidate();

            //Final step to apply the gestures, commit changes
            foreach(ContextNode node in deletedNodes)
            {
                mainInkCanvas.Strokes.Remove(node.Strokes);
                inkAnalyzer.RemoveStrokes(node.Strokes);
            }

            //Reflow all lines
            reflowText(inkAnalyzer, mainInkCanvas, 25);
        }

        private void offsetLineAfterNode(ContextNode node, double offset)
        {
            ContextNodeCollection line = node.ParentNode.SubNodes;
            for(int i = 0; i < line.Count; i++)
            {
                ContextNode sameLineNode = line[i];
                if(sameLineNode.Strokes.GetBounds().X > node.Strokes.GetBounds().X)
                {
                    transposeContextNode(sameLineNode, offset, 0);
                }
            }
        }

        private void reflowText(InkAnalyzer inkAnalyzer, InkCanvas inkCanvas, double width)
        {
            ContextNodeCollection parentList = inkAnalyzer.RootNode.SubNodes;
            foreach(ContextNode node in parentList)
            {
                if(node is WritingRegionNode)
                {
                    ContextNodeCollection paragraphs = node.SubNodes;
                    foreach(ContextNode paragraph in paragraphs)
                    {
                        if(paragraph is ParagraphNode)
                        {
                            reflowParagraph(paragraph as ParagraphNode, inkCanvas, width);
                        }
                    }
                }
            }
        }

        private void reflowParagraph(ParagraphNode node, InkCanvas inkCanvas, double spacing)
        {
            ContextNodeCollection lines = node.SubNodes;
            Rect bounds = node.Strokes.GetBounds();
            List<InkWordNode> resultWords = new List<InkWordNode>();
            double lineHeight = 0;
            //Collect all strokes
            foreach(ContextNode line in lines)
            {
                ContextNodeCollection words = line.SubNodes;
                foreach(ContextNode word in words)
                {
                    lineHeight += word.Strokes.GetBounds().Height;
                    InkWordNode wordNode = word as InkWordNode;
                    resultWords.Add(wordNode);
                }
            }
            lineHeight /= resultWords.Count;

            List<List<InkWordNode>> resultLines = new List<List<InkWordNode>>();
            List<double> lineMaxMidline = new List<double>();
            //Reflow strokes
            double x = 0;
            double maxX = inkCanvas.ActualWidth - bounds.X;
            resultLines.Add(new List<InkWordNode>());
            lineMaxMidline.Add(0);
            foreach(InkWordNode word in resultWords)
            {
                //Does word fit?
                Rect wordBound = word.Strokes.GetBounds();
                if(x + wordBound.Width + spacing > maxX)
                {
                    //Not fitting! Newline
                    x = 0;
                    resultLines.Add(new List<InkWordNode>());
                    lineMaxMidline.Add(0);
                }
                PointCollection mid = word.GetMidline();
                double midlineFromTop = mid[0].Y - wordBound.Y;
                resultLines[resultLines.Count - 1].Add(word);
                if(midlineFromTop > lineMaxMidline[resultLines.Count - 1])
                {
                    lineMaxMidline[resultLines.Count - 1] = midlineFromTop;
                }
            }

            double y = 0;
            int lineNumber = 0;
            foreach(List<InkWordNode> line in resultLines)
            {
                double lineMidline = lineMaxMidline[lineNumber];
                x = 0;
                foreach(InkWordNode word in line)
                {
                    Rect wordBound = word.Strokes.GetBounds();
                    PointCollection mid = word.GetMidline();
                    double midlineFromTop = mid[0].Y - wordBound.Y;
                    double destX = (x + bounds.X);
                    double dx = destX - (wordBound.X);
                    //Match mid
                    double dy = (y + lineMidline + bounds.Y) - (wordBound.Y + midlineFromTop);
                    InkUtils.transposeStrokes(inkAnalyzer, word.Strokes, dx, dy);
                    x += spacing + wordBound.Width;
                }
                y += lineHeight + spacing;
                lineNumber++;
            }
        }

        private void transposeContextNode(ContextNode node, double offsetX, double offsetY)
        {
            foreach(Stroke stroke in node.Strokes)
            {
                Matrix inkTransform = new Matrix();
                inkTransform.Translate(offsetX, offsetY);
                stroke.Transform(inkTransform, false);
            }
        }

        private bool strokeIsHorizontalLine(Stroke stroke)
        {
            Rect bounds = stroke.GetBounds();
            return bounds.Height / bounds.Width < 0.1;
        }

        private bool strokeIsCaret(Stroke stroke)
        {
            if(stroke.StylusPoints.Count() > 3)
            {
                bool hasGoodFirst = false;
                bool hasGoodSecond = false;
                StylusPoint beginningPoint = stroke.StylusPoints.First();
                StylusPoint middlePoint = stroke.StylusPoints[stroke.StylusPoints.Count / 2];
                StylusPoint endPoint = stroke.StylusPoints.Last();
                if(beginningPoint.X < middlePoint.X)
                {
                    double firstExpectedSlope = (middlePoint.Y - beginningPoint.Y) / (middlePoint.X - beginningPoint.X);
                    if(firstExpectedSlope < -0.5 && firstExpectedSlope > -4)
                    {
                        double sum = 0;
                        for(int i = 0; i < stroke.StylusPoints.Count / 2; i++)
                        {
                            StylusPoint point = stroke.StylusPoints[i];
                            double expectedY = beginningPoint.Y + (point.X - beginningPoint.X) * firstExpectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        double offset = sum / stroke.StylusPoints.Count();
                        Debug.WriteLine(offset);
                        if(offset < 100)
                        {
                            hasGoodFirst = true;
                        }
                    }
                }
                if(middlePoint.X < endPoint.X)
                {
                    double secondExpectedSlope = (endPoint.Y - middlePoint.Y) / (endPoint.X - middlePoint.X);
                    if(secondExpectedSlope > 0.5 && secondExpectedSlope < 4)
                    {
                        double sum = 0;
                        for(int i = stroke.StylusPoints.Count / 2; i < stroke.StylusPoints.Count; i++)
                        {
                            StylusPoint point = stroke.StylusPoints[i];
                            double expectedY = middlePoint.Y + (point.X - middlePoint.X) * secondExpectedSlope;
                            sum += Math.Pow(point.Y - expectedY, 2);
                        }
                        double offset = sum / stroke.StylusPoints.Count();
                        Debug.WriteLine(offset);
                        if(offset < 100)
                        {
                            hasGoodSecond = true;
                        }
                    }
                }
                Debug.WriteLine(hasGoodFirst && hasGoodSecond);
                return hasGoodFirst && hasGoodSecond;
            }
            return false;
        }
    }
}
