using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace InkAnalyzerTest.Processors
{
    public class ReflowProcessor : InkProcessor
    {
        private InkCanvas canvas;

        public ReflowProcessor(InkCanvas canvas)
        {
            this.canvas = canvas;
        }

        public void process(InkAnalyzer inkAnalyzer)
        {
            reflowAll(inkAnalyzer, canvas);
        }

        private void reflowAll(InkAnalyzer inkAnalyzer, InkCanvas inkCanvas)
        {
            ContextNodeCollection parentList = inkAnalyzer.RootNode.SubNodes;
            foreach (ContextNode node in parentList)
            {
                if (node is WritingRegionNode)
                {
                    ContextNodeCollection paragraphs = node.SubNodes;
                    foreach (ContextNode paragraph in paragraphs)
                    {
                        if (paragraph is ParagraphNode)
                        {
                            reflowParagraph(paragraph as ParagraphNode, inkAnalyzer, inkCanvas);
                        }
                    }
                }
            }
        }

        private void reflowParagraph(ParagraphNode node, InkAnalyzer inkAnalyzer, InkCanvas inkCanvas)
        {
            ContextNodeCollection lines = node.SubNodes;
            Rect bounds = node.Strokes.GetBounds();
            List<InkWordNode> resultWords = new List<InkWordNode>();
            double lineHeight = 0;
            double spacing = 30;
            //Collect all strokes
            foreach (ContextNode line in lines)
            {
                ContextNodeCollection words = line.SubNodes;
                foreach (ContextNode word in words)
                {
                    lineHeight += word.Strokes.GetBounds().Height;
                    InkWordNode wordNode = word as InkWordNode;
                    resultWords.Add(wordNode);
                }
            }
            lineHeight /= resultWords.Count;

            List<List<InkWordNode>> resultLines = new List<List<InkWordNode>>();
            List<double> lineMaxBaseline = new List<double>();
            //Reflow strokes
            double x = 0;
            double maxX = inkCanvas.ActualWidth - bounds.X;
            resultLines.Add(new List<InkWordNode>());
            lineMaxBaseline.Add(0);
            foreach (InkWordNode word in resultWords)
            {
                //Does word fit?
                Rect wordBound = word.Strokes.GetBounds();
                if (x + wordBound.Width + spacing > maxX)
                {
                    //Not fitting! Newline
                    x = 0;
                    resultLines.Add(new List<InkWordNode>());
                    lineMaxBaseline.Add(0);
                }
                x += spacing + wordBound.Width;
                PointCollection baseline = word.GetBaseline();
                if (baseline != null && baseline.Count > 0)
                {
                    double baselineFromTop = baseline[0].Y - wordBound.Y;
                    resultLines[resultLines.Count - 1].Add(word);
                    if (baselineFromTop > lineMaxBaseline[resultLines.Count - 1])
                    {
                        lineMaxBaseline[resultLines.Count - 1] = baselineFromTop;
                    }
                }
            }

            double y = 0;
            int lineNumber = 0;
            foreach (List<InkWordNode> line in resultLines)
            {
                double lineBaseline = lineMaxBaseline[lineNumber];
                x = 0;
                foreach (InkWordNode word in line)
                {
                    Rect wordBound = word.Strokes.GetBounds();
                    PointCollection baseline = word.GetBaseline();
                    double baselineFromTop = baseline[0].Y - wordBound.Y;
                    double destX = (x + bounds.X);
                    double dx = destX - (wordBound.X);
                    //Match mid
                    double dy = (y + lineBaseline + bounds.Y) - (wordBound.Y + baselineFromTop);
                    InkUtils.transposeStrokes(inkAnalyzer, word.Strokes, dx, dy);
                    x += spacing + wordBound.Width;
                }
                y += lineHeight + spacing;
                lineNumber++;
            }
        }
    }
}
