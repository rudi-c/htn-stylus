using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;

namespace InkAnalyzerTest.Processors
{
    public class NavigationProcessor : InkProcessor
    {
        private Headings mainHeading;

        public NavigationProcessor(Headings mainHeading)
        {
            this.mainHeading = mainHeading;
        }

        public void process(InkAnalyzer inkAnalyzer)
        {
            ContextNodeCollection contextNodeCollection = inkAnalyzer.FindLeafNodes();

            List<Stroke> horizontalLines = InkUtils.findLines(contextNodeCollection);

            List<HeadingItem> headings = findHeadings(horizontalLines, contextNodeCollection);

            //Here is the end result of the headings
            mainHeading.headings = headings;
            //Invalidate and render the headings to the side panel
            mainHeading.invalidate();
        }

        private List<HeadingItem> findHeadings(List<Stroke> horizontalLines, ContextNodeCollection contextNodeCollection)
        {
            //Group lines together into "Heading Groups"
            List<HeadingItem> headings = new List<HeadingItem>();
            foreach (Stroke node1 in horizontalLines)
            {
                List<HeadingItem> intersectHeadings = new List<HeadingItem>();
                foreach (HeadingItem heading in headings)
                {
                    bool intersectsAny = false;
                    foreach (Stroke node2 in heading.lines)
                    {
                        Rect first = node1.GetBounds();
                        Rect second = node2.GetBounds();
                        if (Math.Abs(first.Y - second.Y) < 20 && Math.Abs(first.X - second.X) < 20)
                        {
                            intersectsAny = true;
                            break;
                        }
                    }
                    if (intersectsAny)
                    {
                        intersectHeadings.Add(heading);
                    }
                }

                HeadingItem resultHeading = new HeadingItem();
                foreach (HeadingItem heading in intersectHeadings)
                {
                    resultHeading.lines.AddRange(heading.lines);
                    headings.Remove(heading);
                }
                resultHeading.lines.Add(node1);
                headings.Add(resultHeading);
            }

            //Find words that associate to Heading Groups
            foreach (ContextNode node in contextNodeCollection)
            {
                if (node.Strokes.Count == 0)
                {
                    continue;
                }
                Rect underlineBounds = node.Strokes.GetBounds();
                if (node is InkWordNode)
                {
                    double baseline = (node as InkWordNode).GetBaseline()[0].Y;
                    underlineBounds.Y = baseline;
                    InkWordNode word = node as InkWordNode;

                    foreach (HeadingItem heading in headings)
                    {
                        if (heading.intersects(underlineBounds))
                        {
                            heading.text.Add(word);
                            break;
                        }
                    }
                }
            }

            //Remove bad headings
            List<HeadingItem> actualHeadings = new List<HeadingItem>();
            foreach (HeadingItem heading in headings)
            {
                if (heading.text.Count > 0)
                {
                    actualHeadings.Add(heading);
                }
            }

            return actualHeadings;
        }
    }
}
